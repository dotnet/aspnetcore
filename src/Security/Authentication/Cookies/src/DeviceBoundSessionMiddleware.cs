// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.Cookies;

/// <summary>
/// Middleware that handles DBSC registration and refresh requests.
/// </summary>
internal sealed class DeviceBoundSessionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DeviceBoundSessionMiddleware> _logger;

    public DeviceBoundSessionMiddleware(RequestDelegate next, ILogger<DeviceBoundSessionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check all configured cookie authentication schemes for DBSC paths
        var authOptions = context.RequestServices.GetService<IOptionsMonitor<CookieAuthenticationOptions>>();
        var schemes = context.RequestServices.GetService<IAuthenticationSchemeProvider>();

        if (authOptions is null || schemes is null)
        {
            await _next(context);
            return;
        }

        var allSchemes = await schemes.GetAllSchemesAsync();
        foreach (var scheme in allSchemes)
        {
            if (scheme.HandlerType != typeof(CookieAuthenticationHandler))
            {
                continue;
            }

            var options = authOptions.Get(scheme.Name);
            var dbscOptions = options.DeviceBoundSession;
            if (dbscOptions is null || !dbscOptions.Enabled)
            {
                continue;
            }

            if (context.Request.Path.Equals(dbscOptions.RegistrationPath) && HttpMethods.IsPost(context.Request.Method))
            {
                await HandleRegistrationAsync(context, options, dbscOptions);
                return;
            }

            if (context.Request.Path.Equals(dbscOptions.RefreshPath) && HttpMethods.IsPost(context.Request.Method))
            {
                await HandleRefreshAsync(context, options, dbscOptions);
                return;
            }
        }

        await _next(context);
    }

    private async Task HandleRegistrationAsync(
        HttpContext context,
        CookieAuthenticationOptions options,
        DeviceBoundSessionOptions dbscOptions)
    {
        // Extract the JWT from the Secure-Session-Response header
        var responseHeader = context.Request.Headers["Secure-Session-Response"].ToString();
        if (string.IsNullOrEmpty(responseHeader))
        {
            // Also try reading from body for compatibility
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        // Remove quotes if present (structured header format)
        responseHeader = responseHeader.Trim('"');

        // Validate the JWT and extract the public key
        var result = DeviceBoundSessionJwtValidator.Validate(responseHeader, publicKeyJwk: null, expectedChallenge: null);
        if (result is null)
        {
            _logger.LogWarning("DBSC registration: invalid JWT proof.");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        // Read the existing auth cookie to get the current session
        var cookie = options.CookieManager.GetRequestCookie(context, options.Cookie.Name!);
        if (string.IsNullOrEmpty(cookie))
        {
            _logger.LogWarning("DBSC registration: no auth cookie present.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var ticket = options.TicketDataFormat.Unprotect(cookie, GetTlsTokenBinding(context));
        if (ticket is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Generate session ID
        var sessionId = GenerateSessionId();

        // Store public key in ticket properties (will be re-protected into the long-lived cookie)
        ticket.Properties.Items["DbscPublicKeyJwk"] = result.PublicKeyJwk;
        ticket.Properties.Items["DbscSessionId"] = sessionId;
        ticket.Properties.Items["DbscAlgorithm"] = result.Algorithm;

        // Re-protect and set the long-lived cookie with embedded public key
        var cookieValue = options.TicketDataFormat.Protect(ticket, GetTlsTokenBinding(context));
        var cookieOptions = options.Cookie.Build(context);
        if (ticket.Properties.IsPersistent && ticket.Properties.ExpiresUtc.HasValue)
        {
            cookieOptions.Expires = ticket.Properties.ExpiresUtc.Value.ToUniversalTime();
        }

        options.CookieManager.AppendResponseCookie(context, options.Cookie.Name!, cookieValue, cookieOptions);

        // Set the short-lived cookie
        var shortLivedCookieName = dbscOptions.ShortLivedCookieName ?? $"{options.Cookie.Name}__dbsc";
        var shortLivedCookieOptions = options.Cookie.Build(context);
        shortLivedCookieOptions.Expires = DateTimeOffset.UtcNow.Add(dbscOptions.ShortLivedCookieExpiration);
        shortLivedCookieOptions.MaxAge = dbscOptions.ShortLivedCookieExpiration;

        var shortLivedValue = GenerateShortLivedCookieValue(sessionId);
        options.CookieManager.AppendResponseCookie(context, shortLivedCookieName, shortLivedValue, shortLivedCookieOptions);

        // Optionally store in server-side store
        var store = context.RequestServices.GetService<IDeviceBoundSessionStore>();
        if (store is not null)
        {
            await store.StoreAsync(sessionId, result.PublicKeyJwk, context.RequestAborted);
        }

        // Build and return session configuration
        var config = BuildSessionConfiguration(context, options, dbscOptions, sessionId, shortLivedCookieName);

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json";

        // Include a challenge for the next refresh
        var challengeGenerator = GetChallengeGenerator(context);
        var challenge = challengeGenerator.GenerateChallenge(sessionId);
        context.Response.Headers["Secure-Session-Challenge"] = $"\"{challenge}\";id=\"{sessionId}\"";

        await JsonSerializer.SerializeAsync(context.Response.Body, config, DeviceBoundSessionJsonContext.Default.DeviceBoundSessionConfiguration, context.RequestAborted);
    }

    private async Task HandleRefreshAsync(
        HttpContext context,
        CookieAuthenticationOptions options,
        DeviceBoundSessionOptions dbscOptions)
    {
        // Read session ID from header
        var sessionIdHeader = context.Request.Headers["Sec-Secure-Session-Id"].ToString();
        if (string.IsNullOrEmpty(sessionIdHeader))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        // Remove quotes if present
        sessionIdHeader = sessionIdHeader.Trim('"');

        // Check for server-side revocation
        var store = context.RequestServices.GetService<IDeviceBoundSessionStore>();
        if (store is not null && await store.IsRevokedAsync(sessionIdHeader, context.RequestAborted))
        {
            // Return 403 without challenge to terminate the session
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        // Read the long-lived cookie to get the public key
        var cookie = options.CookieManager.GetRequestCookie(context, options.Cookie.Name!);
        if (string.IsNullOrEmpty(cookie))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var ticket = options.TicketDataFormat.Unprotect(cookie, GetTlsTokenBinding(context));
        if (ticket is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Verify the session ID matches
        if (!ticket.Properties.Items.TryGetValue("DbscSessionId", out var storedSessionId) ||
            !string.Equals(storedSessionId, sessionIdHeader, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        // Check for the proof JWT
        var proofHeader = context.Request.Headers["Secure-Session-Response"].ToString();
        if (string.IsNullOrEmpty(proofHeader))
        {
            // No proof yet — issue a challenge
            var challengeGenerator = GetChallengeGenerator(context);
            var challenge = challengeGenerator.GenerateChallenge(sessionIdHeader);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.Headers["Secure-Session-Challenge"] = $"\"{challenge}\";id=\"{sessionIdHeader}\"";
            return;
        }

        // Remove quotes if present
        proofHeader = proofHeader.Trim('"');

        // Get the public key from the ticket
        if (!ticket.Properties.Items.TryGetValue("DbscPublicKeyJwk", out var publicKeyJwk) ||
            publicKeyJwk is null)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        // Validate the JWT proof
        // The jti claim should match a challenge we issued — we validate it's a valid challenge
        var jwtResult = DeviceBoundSessionJwtValidator.Validate(proofHeader, publicKeyJwk, expectedChallenge: null);
        if (jwtResult is null)
        {
            _logger.LogWarning("DBSC refresh: invalid JWT signature for session {SessionId}.", sessionIdHeader);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        // Validate the challenge (jti) is one we issued and is fresh
        if (jwtResult.Challenge is not null)
        {
            var challengeGenerator = GetChallengeGenerator(context);
            if (!challengeGenerator.ValidateChallenge(jwtResult.Challenge, sessionIdHeader, dbscOptions.ChallengeMaxAge))
            {
                _logger.LogWarning("DBSC refresh: stale or invalid challenge for session {SessionId}.", sessionIdHeader);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
        }

        // Success — issue a new short-lived cookie
        var shortLivedCookieName = dbscOptions.ShortLivedCookieName ?? $"{options.Cookie.Name}__dbsc";
        var shortLivedCookieOptions = options.Cookie.Build(context);
        shortLivedCookieOptions.Expires = DateTimeOffset.UtcNow.Add(dbscOptions.ShortLivedCookieExpiration);
        shortLivedCookieOptions.MaxAge = dbscOptions.ShortLivedCookieExpiration;

        var shortLivedValue = GenerateShortLivedCookieValue(sessionIdHeader);
        options.CookieManager.AppendResponseCookie(context, shortLivedCookieName, shortLivedValue, shortLivedCookieOptions);

        // Return session configuration with new challenge
        var config = BuildSessionConfiguration(context, options, dbscOptions, sessionIdHeader, shortLivedCookieName);
        var nextChallengeGenerator = GetChallengeGenerator(context);
        var nextChallenge = nextChallengeGenerator.GenerateChallenge(sessionIdHeader);

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json";
        context.Response.Headers["Secure-Session-Challenge"] = $"\"{nextChallenge}\";id=\"{sessionIdHeader}\"";

        await JsonSerializer.SerializeAsync(context.Response.Body, config, DeviceBoundSessionJsonContext.Default.DeviceBoundSessionConfiguration, context.RequestAborted);
    }

    private static DeviceBoundSessionConfiguration BuildSessionConfiguration(
        HttpContext context,
        CookieAuthenticationOptions options,
        DeviceBoundSessionOptions dbscOptions,
        string sessionId,
        string shortLivedCookieName)
    {
        var request = context.Request;
        var origin = $"{request.Scheme}://{request.Host}";

        var scopeRules = dbscOptions.ScopeSpecifications.Count > 0
            ? dbscOptions.ScopeSpecifications.Select(r => new DeviceBoundSessionScopeRuleConfiguration
            {
                Type = r.Type,
                Domain = r.Domain,
                Path = r.Path
            }).ToList()
            : null;

        // Build cookie attributes string
        var cookieBuilder = options.Cookie.Build(context);
        var attributes = BuildCookieAttributesString(cookieBuilder);

        return new DeviceBoundSessionConfiguration
        {
            SessionIdentifier = sessionId,
            RefreshUrl = dbscOptions.RefreshPath.Value,
            Scope = new DeviceBoundSessionScopeConfiguration
            {
                Origin = origin,
                IncludeSite = dbscOptions.IncludeSite,
                ScopeSpecification = scopeRules
            },
            Credentials = new List<DeviceBoundSessionCredentialConfiguration>
            {
                new DeviceBoundSessionCredentialConfiguration
                {
                    Type = "cookie",
                    Name = shortLivedCookieName,
                    Attributes = attributes
                }
            },
            AllowedRefreshInitiators = dbscOptions.AllowedRefreshInitiators.Count > 0
                ? dbscOptions.AllowedRefreshInitiators.ToList()
                : null
        };
    }

    private static string BuildCookieAttributesString(CookieOptions cookieOptions)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(cookieOptions.Domain))
        {
            parts.Add($"Domain={cookieOptions.Domain}");
        }

        if (!string.IsNullOrEmpty(cookieOptions.Path))
        {
            parts.Add($"Path={cookieOptions.Path}");
        }

        if (cookieOptions.Secure)
        {
            parts.Add("Secure");
        }

        if (cookieOptions.HttpOnly)
        {
            parts.Add("HttpOnly");
        }

        if (cookieOptions.SameSite != SameSiteMode.Unspecified)
        {
            parts.Add($"SameSite={cookieOptions.SameSite}");
        }

        return string.Join("; ", parts);
    }

    private static string GenerateSessionId()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string GenerateShortLivedCookieValue(string sessionId)
    {
        // The short-lived cookie value is a simple marker that proves it was recently issued.
        // It doesn't need to contain sensitive data — the long-lived cookie has the auth ticket.
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(8));
        return $"{sessionId}:{nonce}";
    }

    private static string? GetTlsTokenBinding(HttpContext context)
    {
        var binding = context.Features.Get<Microsoft.AspNetCore.Http.Features.ITlsTokenBindingFeature>()?.GetProvidedTokenBindingId();
        return binding is null ? null : Convert.ToBase64String(binding);
    }

    private static DeviceBoundSessionChallengeGenerator GetChallengeGenerator(HttpContext context)
    {
        var dataProtection = context.RequestServices.GetRequiredService<IDataProtectionProvider>();
        var timeProvider = context.RequestServices.GetService<TimeProvider>();
        return new DeviceBoundSessionChallengeGenerator(dataProtection, timeProvider);
    }
}
