// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

/// <summary>
/// Authentication handler that implements the Device Bound Session Credentials (DBSC) protocol.
/// Handles registration and refresh endpoints, delegating cookie management to separate cookie schemes.
/// </summary>
public class DeviceBoundSessionHandler : AuthenticationHandler<DeviceBoundSessionOptions>, IAuthenticationRequestHandler
{
    private readonly DeviceBoundSessionChallengeProtector _challengeProtector;

    /// <summary>
    /// Initializes a new instance of <see cref="DeviceBoundSessionHandler"/>.
    /// </summary>
    public DeviceBoundSessionHandler(
        IOptionsMonitor<DeviceBoundSessionOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IDataProtectionProvider dataProtectionProvider)
        : base(options, logger, encoder)
    {
        _challengeProtector = new DeviceBoundSessionChallengeProtector(dataProtectionProvider);
    }

    /// <summary>
    /// The handler does not authenticate normal requests — cookie handlers do that.
    /// </summary>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        => Task.FromResult(AuthenticateResult.NoResult());

    /// <summary>
    /// Handles DBSC registration and refresh requests if the path matches.
    /// </summary>
    public async Task<bool> HandleRequestAsync()
    {
        if (HttpMethods.IsPost(Request.Method))
        {
            if (Request.Path.Equals(Options.RegistrationPath))
            {
                await HandleRegistrationAsync();
                return true;
            }

            if (Request.Path.Equals(Options.RefreshPath))
            {
                await HandleRefreshAsync();
                return true;
            }
        }

        return false;
    }

    private async Task HandleRegistrationAsync()
    {
        // Extract the JWT proof from the Secure-Session-Response header
        var responseHeader = Request.Headers["Secure-Session-Response"].ToString();
        if (string.IsNullOrEmpty(responseHeader))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        responseHeader = responseHeader.Trim('"');

        // Validate the JWT and extract the public key (registration: no existing key to check against)
        var jwtResult = await DeviceBoundSessionJwtValidator.ValidateAsync(responseHeader, publicKeyJwk: null, expectedChallenge: null);
        if (jwtResult is null)
        {
            Logger.LogWarning("DBSC registration: invalid JWT proof.");
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        // Authenticate against the registration source scheme (the long-lived sign-in cookie)
        var authResult = await Context.AuthenticateAsync(Options.RegistrationSourceScheme);
        if (!authResult.Succeeded || authResult.Principal is null)
        {
            Logger.LogWarning("DBSC registration: no valid authentication from source scheme.");
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var principal = authResult.Principal;
        var properties = authResult.Properties ?? new AuthenticationProperties();

        if (jwtResult.Challenge is null || !ValidateRegistrationChallenge(jwtResult.Challenge, principal))
        {
            Logger.LogWarning("DBSC registration: invalid challenge.");
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        // Generate a session ID
        var sessionId = GenerateSessionId();

        // Store public key and session info in properties for the refresh cookie
        var refreshProperties = new AuthenticationProperties();
        foreach (var item in properties.Items)
        {
            refreshProperties.Items[item.Key] = item.Value;
        }
        refreshProperties.Items["DbscPublicKeyJwk"] = jwtResult.PublicKeyJwk;
        refreshProperties.Items["DbscSessionId"] = sessionId;
        refreshProperties.Items["DbscAlgorithm"] = jwtResult.Algorithm;
        refreshProperties.IsPersistent = true;

        // 1. Stamp the refresh cookie (path-scoped stash with ticket + public key)
        await Context.SignInAsync(Options.RefreshScheme, principal, refreshProperties);

        // 2. Stamp the short-lived session cookie
        var sessionProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.Add(Options.ShortLivedCookieExpiration),
            IssuedUtc = DateTimeOffset.UtcNow,
        };
        await Context.SignInAsync(Options.SessionScheme, principal, sessionProperties);

        // 3. Delete the long-lived source cookie (exchange complete)
        await Context.SignOutAsync(Options.RegistrationSourceScheme);

        // Build and return session configuration JSON
        var config = BuildSessionConfiguration(sessionId);

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "application/json";

        // Include a challenge for the next refresh
        var challenge = GenerateRefreshChallenge(principal, sessionId);
        Response.Headers["Secure-Session-Challenge"] = $"\"{challenge}\";id=\"{sessionId}\"";

        await JsonSerializer.SerializeAsync(Response.Body, config, DeviceBoundSessionJsonContext.Default.DeviceBoundSessionConfiguration, Context.RequestAborted);
    }

    private async Task HandleRefreshAsync()
    {
        // Read session ID from header
        var sessionIdHeader = Request.Headers["Sec-Secure-Session-Id"].ToString();
        if (string.IsNullOrEmpty(sessionIdHeader))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        sessionIdHeader = sessionIdHeader.Trim('"');

        // Authenticate against the refresh scheme (path-scoped refresh cookie)
        var authResult = await Context.AuthenticateAsync(Options.RefreshScheme);
        if (!authResult.Succeeded || authResult.Principal is null)
        {
            Logger.LogWarning("DBSC refresh: no valid refresh cookie for session {SessionId}.", sessionIdHeader);
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var properties = authResult.Properties;

        // Verify the session ID matches what's in the refresh cookie
        if (properties is null ||
            !properties.Items.TryGetValue("DbscSessionId", out var storedSessionId) ||
            !string.Equals(storedSessionId, sessionIdHeader, StringComparison.Ordinal))
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        // Get the public key from the refresh cookie
        if (!properties.Items.TryGetValue("DbscPublicKeyJwk", out var publicKeyJwk) || publicKeyJwk is null)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        // Check for the proof JWT
        var proofHeader = Request.Headers["Secure-Session-Response"].ToString();
        if (string.IsNullOrEmpty(proofHeader))
        {
            // No proof yet — issue a challenge (first leg of refresh)
            var challenge = GenerateRefreshChallenge(authResult.Principal, sessionIdHeader);
            Response.StatusCode = StatusCodes.Status403Forbidden;
            Response.Headers["Secure-Session-Challenge"] = $"\"{challenge}\";id=\"{sessionIdHeader}\"";
            return;
        }

        proofHeader = proofHeader.Trim('"');

        // Validate the JWT proof against the public key from the refresh cookie
        var jwtResult = await DeviceBoundSessionJwtValidator.ValidateAsync(proofHeader, publicKeyJwk, expectedChallenge: null);
        if (jwtResult is null)
        {
            Logger.LogWarning("DBSC refresh: invalid JWT signature for session {SessionId}.", sessionIdHeader);
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        // Validate the challenge (jti) is one we issued and is fresh.
        if (jwtResult.Challenge is null || !ValidateRefreshChallenge(jwtResult.Challenge, authResult.Principal, sessionIdHeader))
        {
            Logger.LogWarning("DBSC refresh: stale or invalid challenge for session {SessionId}.", sessionIdHeader);
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        // Success — stamp a fresh short-lived session cookie
        var sessionProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.Add(Options.ShortLivedCookieExpiration),
            IssuedUtc = DateTimeOffset.UtcNow,
        };
        await Context.SignInAsync(Options.SessionScheme, authResult.Principal, sessionProperties);

        // Return session configuration with new challenge
        var config = BuildSessionConfiguration(sessionIdHeader);
        var nextChallenge = GenerateRefreshChallenge(authResult.Principal, sessionIdHeader);

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "application/json";
        Response.Headers["Secure-Session-Challenge"] = $"\"{nextChallenge}\";id=\"{sessionIdHeader}\"";

        await JsonSerializer.SerializeAsync(Response.Body, config, DeviceBoundSessionJsonContext.Default.DeviceBoundSessionConfiguration, Context.RequestAborted);
    }

    private DeviceBoundSessionConfiguration BuildSessionConfiguration(string sessionId)
    {
        var origin = $"{Request.Scheme}://{Request.Host}";

        List<DeviceBoundSessionScopeRuleConfiguration>? scopeRules = null;
        if (Options.ScopeSpecifications.Count > 0)
        {
            scopeRules = Options.ScopeSpecifications.Select(r => new DeviceBoundSessionScopeRuleConfiguration
            {
                Type = r.Type,
                Domain = r.Domain,
                Path = r.Path,
            }).ToList();
        }

        // The credential cookie name is based on the session scheme
        // We need to resolve it from the cookie options for that scheme
        var sessionCookieName = ResolveSessionCookieName();

        return new DeviceBoundSessionConfiguration
        {
            SessionIdentifier = sessionId,
            RefreshUrl = Options.RefreshPath.Value,
            Scope = new DeviceBoundSessionScopeConfiguration
            {
                Origin = origin,
                IncludeSite = Options.IncludeSite,
                ScopeSpecification = scopeRules,
            },
            Credentials = new List<DeviceBoundSessionCredentialConfiguration>
            {
                new DeviceBoundSessionCredentialConfiguration
                {
                    Type = "cookie",
                    Name = sessionCookieName,
                    Attributes = "Secure; HttpOnly; SameSite=Lax; Path=/",
                }
            },
        };
    }

    private string ResolveSessionCookieName()
    {
        // Try to get the cookie name from the session scheme's cookie options
        var cookieOptionsMonitor = Context.RequestServices.GetService(
            typeof(IOptionsMonitor<CookieAuthenticationOptions>)) as IOptionsMonitor<Cookies.CookieAuthenticationOptions>;
        if (cookieOptionsMonitor is not null && Options.SessionScheme is not null)
        {
            var cookieOptions = cookieOptionsMonitor.Get(Options.SessionScheme);
            if (cookieOptions.Cookie.Name is not null)
            {
                return cookieOptions.Cookie.Name;
            }
        }
        return ".AspNetCore.Dbsc.Session";
    }

    private string GenerateRegistrationChallenge(ClaimsPrincipal principal)
        => _challengeProtector.GenerateRegistrationChallenge(principal, Options.ChallengeMaxAge);

    private bool ValidateRegistrationChallenge(string challenge, ClaimsPrincipal principal)
        => _challengeProtector.TryValidateRegistrationChallenge(challenge, principal);

    private string GenerateRefreshChallenge(ClaimsPrincipal principal, string sessionId)
        => _challengeProtector.GenerateRefreshChallenge(principal, sessionId, Options.ChallengeMaxAge);

    private bool ValidateRefreshChallenge(string challenge, ClaimsPrincipal principal, string expectedSessionId)
        => _challengeProtector.TryValidateRefreshChallenge(challenge, principal, expectedSessionId);

    private static string GenerateSessionId()
    {
        Span<byte> bytes = stackalloc byte[24];
        RandomNumberGenerator.Fill(bytes);
        return WebEncoders.Base64UrlEncode(bytes);
    }
}
