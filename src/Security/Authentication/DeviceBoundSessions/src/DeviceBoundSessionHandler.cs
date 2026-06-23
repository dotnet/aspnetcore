// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
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
[Experimental("ASP0030", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public class DeviceBoundSessionHandler : AuthenticationHandler<DeviceBoundSessionOptions>, IAuthenticationRequestHandler
{
    private readonly DeviceBoundSessionChallengeProtector _challengeProtector;
    private readonly DeviceBoundSessionJwtValidator _jwtValidator;

    /// <summary>
    /// Initializes a new instance of <see cref="DeviceBoundSessionHandler"/>.
    /// </summary>
    /// <param name="options">The monitor for <see cref="DeviceBoundSessionOptions"/> instances.</param>
    /// <param name="logger">The <see cref="ILoggerFactory"/> used to create loggers.</param>
    /// <param name="encoder">The <see cref="UrlEncoder"/> used to encode URLs.</param>
    /// <param name="dataProtectionProvider">The <see cref="IDataProtectionProvider"/> used to protect and unprotect registration challenges.</param>
    public DeviceBoundSessionHandler(
        IOptionsMonitor<DeviceBoundSessionOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IDataProtectionProvider dataProtectionProvider)
        : base(options, logger, encoder)
    {
        _challengeProtector = new DeviceBoundSessionChallengeProtector(dataProtectionProvider, logger.CreateLogger<DeviceBoundSessionChallengeProtector>());
        _jwtValidator = new DeviceBoundSessionJwtValidator(logger.CreateLogger<DeviceBoundSessionJwtValidator>());
    }

    /// <summary>
    /// The handler does not authenticate normal requests — cookie handlers do that.
    /// </summary>
    /// <returns>A completed task whose result is always <see cref="AuthenticateResult.NoResult"/>.</returns>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        => Task.FromResult(AuthenticateResult.NoResult());

    /// <summary>
    /// Handles DBSC registration and refresh requests if the path matches.
    /// </summary>
    /// <returns>
    /// A task whose result is <see langword="true"/> when the request matched a DBSC registration or
    /// refresh endpoint and was handled (the pipeline should short-circuit); otherwise <see langword="false"/>.
    /// </returns>
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
        var proofHeader = Request.Headers[DeviceBoundSessionConstants.Headers.Proof].ToString();
        if (string.IsNullOrEmpty(proofHeader))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        proofHeader = proofHeader.Trim('"');

        // Validate the JWT and extract the public key (registration: no existing key to check against)
        var jwtResult = await _jwtValidator.ValidateAsync(proofHeader, publicKeyJwk: null, expectedChallenge: null);
        if (jwtResult is null)
        {
            // The validator logs the specific reason (malformed / wrong typ / unsupported alg / bad signature).
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        // Authenticate against the registration source scheme (the long-lived sign-in cookie)
        var authResult = await Context.AuthenticateAsync(Options.RegistrationSourceScheme);
        if (!authResult.Succeeded || authResult.Principal is null)
        {
            Logger.RegistrationNoSourceAuthentication();
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var principal = authResult.Principal;
        var properties = authResult.Properties ?? new AuthenticationProperties();

        if (jwtResult.Challenge is null)
        {
            Logger.RegistrationChallengeMissing();
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        if (!ValidateRegistrationChallenge(jwtResult.Challenge, principal))
        {
            // The protector logs the specific reason (undecryptable / malformed / principal-mismatch).
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

        // Anchor the refresh cookie's lifetime to the original sign-in cookie so the bound session
        // never outlives the credential it was exchanged for. Copying the source ticket's IssuedUtc/
        // ExpiresUtc makes the cookie handler honor them (instead of starting a fresh ExpireTimeSpan
        // window at registration time). Falls back to the handler default if the source has no expiry.
        refreshProperties.IssuedUtc = properties.IssuedUtc;
        refreshProperties.ExpiresUtc = properties.ExpiresUtc;

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

        // Build and return session instructions JSON.
        var instructions = BuildSessionInstruction(sessionId);

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "application/json";

        await JsonSerializer.SerializeAsync(Response.Body, instructions, DeviceBoundSessionJsonContext.Default.SessionInstruction, Context.RequestAborted);
    }

    private async Task HandleRefreshAsync()
    {
        // Read session ID from header
        var sessionIdHeader = Request.Headers[DeviceBoundSessionConstants.Headers.SessionId].ToString();
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
            Logger.RefreshNoCookie(sessionIdHeader);
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
        var proofHeader = Request.Headers[DeviceBoundSessionConstants.Headers.Proof].ToString();
        if (string.IsNullOrEmpty(proofHeader))
        {
            // No proof yet — issue a challenge (first leg of refresh)
            var challenge = GenerateRefreshChallenge(authResult.Principal, sessionIdHeader);
            Response.StatusCode = StatusCodes.Status403Forbidden;
            Response.Headers[DeviceBoundSessionConstants.Headers.Challenge] = $"\"{challenge}\";id=\"{sessionIdHeader}\"";
            return;
        }

        proofHeader = proofHeader.Trim('"');

        // Validate the JWT proof against the public key from the refresh cookie
        var jwtResult = await _jwtValidator.ValidateAsync(proofHeader, publicKeyJwk, expectedChallenge: null);
        if (jwtResult is null)
        {
            // The validator logs the specific reason (malformed / wrong typ / unsupported alg / bad signature).
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        // Validate the challenge (jti). The protector logs the specific reason
        // (undecryptable / malformed / principal- or session-mismatch); a missing jti is invalid.
        var challengeValid = jwtResult.Challenge is not null
            && ValidateRefreshChallenge(jwtResult.Challenge, authResult.Principal, sessionIdHeader);

        if (!challengeValid)
        {
            // Re-issue a fresh, correctly-bound challenge so the client can immediately
            // retry (the DBSC 403 + Secure-Session-Challenge handshake).
            // This is curative, not a futile retry: the signature and refresh cookie already validated and are
            // unchanged on retry, so the stale/expired/version-skewed challenge was the only failing input and a
            // fresh nonce fixes it. Persistent client/server faults can't recover but are bounded by the
            // client's challenge quota, which ends in a clean re-login.
            var retryChallenge = GenerateRefreshChallenge(authResult.Principal, sessionIdHeader);
            Response.StatusCode = StatusCodes.Status403Forbidden;
            Response.Headers[DeviceBoundSessionConstants.Headers.Challenge] = $"\"{retryChallenge}\";id=\"{sessionIdHeader}\"";
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

        // NOTE: Returning the Session Instruction (config JSON) on a refresh 200 is OPTIONAL per the
        // DBSC spec — the browser already has the instructions from registration, and we verified the
        // session keeps working when the refresh body is empty. We currently re-send it every time for
        // simplicity. A future optimization could omit it on the common path and only send it when the
        // instructions actually need to change — e.g. to update scope/credentials, narrow access, or
        // force a logout/cleanup everywhere by returning updated (or session-ending) instructions.
        var instructions = BuildSessionInstruction(sessionIdHeader);

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "application/json";

        await JsonSerializer.SerializeAsync(Response.Body, instructions, DeviceBoundSessionJsonContext.Default.SessionInstruction, Context.RequestAborted);
    }

    private SessionInstruction BuildSessionInstruction(string sessionId)
    {
        var origin = $"{Request.Scheme}://{Request.Host}";

        List<SessionScopeRule>? scopeRules = null;
        if (Options.ScopeSpecifications.Count > 0)
        {
            scopeRules = Options.ScopeSpecifications.Select(r => new SessionScopeRule
            {
                Type = r.Type,
                Domain = r.Domain,
                Path = r.Path,
            }).ToList();
        }

        // The credential cookie name is based on the session scheme
        // We need to resolve it from the cookie options for that scheme
        var sessionCookieName = ResolveSessionCookieName();

        return new SessionInstruction
        {
            SessionIdentifier = sessionId,
            RefreshUrl = Options.RefreshPath.Value,
            Scope = new SessionScope
            {
                Origin = origin,
                IncludeSite = Options.IncludeSite,
                ScopeSpecification = scopeRules,
            },
            Credentials = new List<SessionCredential>
            {
                new SessionCredential
                {
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
