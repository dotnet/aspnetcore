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
using Microsoft.Net.Http.Headers;
using NetSameSiteMode = Microsoft.Net.Http.Headers.SameSiteMode;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

/// <summary>
/// Authentication handler that implements the Device Bound Session Credentials (DBSC) protocol.
/// Handles registration and refresh endpoints, delegating cookie management to separate cookie schemes.
/// </summary>
[Experimental("ASP0031", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public class DeviceBoundSessionHandler : AuthenticationHandler<DeviceBoundSessionOptions>, IAuthenticationRequestHandler
{
    private readonly DeviceBoundSessionChallengeProtector _challengeProtector;
    private readonly DeviceBoundSessionJwtValidator _jwtValidator;
    private readonly IOptionsMonitor<CookieAuthenticationOptions> _cookieOptionsMonitor;

    /// <summary>
    /// Initializes a new instance of <see cref="DeviceBoundSessionHandler"/>.
    /// </summary>
    /// <param name="options">The monitor for <see cref="DeviceBoundSessionOptions"/> instances.</param>
    /// <param name="logger">The <see cref="ILoggerFactory"/> used to create loggers.</param>
    /// <param name="encoder">The <see cref="UrlEncoder"/> used to encode URLs.</param>
    /// <param name="dataProtectionProvider">The <see cref="IDataProtectionProvider"/> used to protect and unprotect registration challenges.</param>
    /// <param name="cookieOptionsMonitor">The monitor for <see cref="CookieAuthenticationOptions"/> used to resolve the session cookie configuration.</param>
    public DeviceBoundSessionHandler(
        IOptionsMonitor<DeviceBoundSessionOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IDataProtectionProvider dataProtectionProvider,
        IOptionsMonitor<CookieAuthenticationOptions> cookieOptionsMonitor)
        : base(options, logger, encoder)
    {
        _challengeProtector = new DeviceBoundSessionChallengeProtector(dataProtectionProvider, logger.CreateLogger<DeviceBoundSessionChallengeProtector>());
        _jwtValidator = new DeviceBoundSessionJwtValidator(logger.CreateLogger<DeviceBoundSessionJwtValidator>());
        _cookieOptionsMonitor = cookieOptionsMonitor;
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
        var jwtResult = await _jwtValidator.ValidateAsync(proofHeader, publicKeyJwk: null);
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

        // The refresh cookie replaces the deleted source cookie and preserves its remaining
        // lifetime, persistence, and refresh policy. Only an already-issued short session cookie
        // can extend ordinary authenticated access beyond the refresh cookie's final expiry.
        var refreshProperties = properties.Clone();
        refreshProperties.RedirectUri = null;
        refreshProperties.Items["DbscPublicKeyJwk"] = jwtResult.PublicKeyJwk;
        refreshProperties.Items["DbscSessionId"] = sessionId;
        refreshProperties.Items["DbscAlgorithm"] = jwtResult.Algorithm;

        // 1. Stamp the refresh cookie (path-scoped stash with ticket + public key)
        await Context.SignInAsync(Options.RefreshScheme, principal, refreshProperties);

        // 2. Stamp the short-lived session cookie
        var now = TimeProvider.GetUtcNow();
        var sessionProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = now.Add(Options.ShortLivedCookieExpiration),
            IssuedUtc = now,
        };
        await Context.SignInAsync(Options.SessionScheme, principal, sessionProperties);

        // 3. Delete the long-lived source cookie (exchange complete). Flag this as the DBSC
        // registration exchange so the source scheme's sign-out hook does not treat it as a user
        // logout and clear the session and refresh cookies we just minted.
        Context.Items[DeviceBoundSessionCookieEvents.RegistrationExchangeItemKey] = true;
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
        var jwtResult = await _jwtValidator.ValidateAsync(proofHeader, publicKeyJwk);
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
        var now = TimeProvider.GetUtcNow();
        var sessionProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = now.Add(Options.ShortLivedCookieExpiration),
            IssuedUtc = now,
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

        var scopeRules = Options.ScopeSpecifications.Select(r => new SessionScopeRule
        {
            Type = r.Type,
            Domain = r.Domain,
            Path = r.Path,
        }).ToList();

        // The credential cookie name and attributes are derived from the session scheme's cookie
        // options so the session instruction stays in lock-step with the cookie we actually emit.
        // A wrong name would silently break DBSC (the browser would bind to a cookie we never set),
        // so we fail loudly rather than emit a guessed default that can never match.
        var sessionCookieOptions = ResolveSessionCookieOptions();
        var sessionCookieName = sessionCookieOptions.Cookie.Name
            ?? throw new InvalidOperationException(
                $"The session cookie scheme '{Options.SessionScheme}' has no configured cookie name; cannot build a valid DBSC session instruction.");

        return new SessionInstruction
        {
            SessionIdentifier = sessionId,
            RefreshUrl = Request.PathBase.Add(Options.RefreshPath).ToUriComponent(),
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
                    Attributes = BuildCredentialAttributes(sessionCookieOptions),
                }
            },
            AllowedRefreshInitiators = [.. Options.AllowedRefreshInitiators],
        };
    }

    private CookieAuthenticationOptions ResolveSessionCookieOptions()
    {
        if (Options.SessionScheme is null)
        {
            throw new InvalidOperationException(
                $"{nameof(DeviceBoundSessionOptions.SessionScheme)} must be configured to build a DBSC session instruction.");
        }

        return _cookieOptionsMonitor.Get(Options.SessionScheme);
    }

    private string BuildCredentialAttributes(CookieAuthenticationOptions cookieOptions)
    {
        // Build the cookie exactly as the session cookie handler will, so the advertised attributes —
        // including the path, which the request path base is applied to — match the cookie we emit.
        var cookie = cookieOptions.Cookie.Build(Context);
        var header = new SetCookieHeaderValue(string.Empty, string.Empty)
        {
            Domain = cookie.Domain,
            Path = string.IsNullOrEmpty(cookie.Path) ? "/" : cookie.Path,
            Secure = cookie.Secure,
            HttpOnly = cookie.HttpOnly,
            SameSite = (NetSameSiteMode)cookie.SameSite,
        };

        var value = header.ToString();
        if (!value.StartsWith("=; ", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Set-Cookie credential serialization did not produce the expected empty cookie prefix.");
        }

        return value[3..];
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
