#pragma warning disable ASP0030 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

/// <summary>
/// Writes the DBSC <c>Secure-Session-Registration</c> response header, advertising the supported
/// signing algorithms and a fresh registration challenge bound to a principal. Shared by the
/// cookie-events wiring paths (the inline <c>OnSigningIn</c> delegate installed by
/// <see cref="PostConfigureDeviceBoundSessionCookieOptions"/> and the
/// <see cref="DeviceBoundSessionCookieEvents"/> wrapper) and by the public
/// <see cref="DeviceBoundSessionHttpContextExtensions"/> on-demand entry point.
/// </summary>
internal static class DeviceBoundSessionRegistrationHeader
{
    /// <summary>
    /// Emits the <c>Secure-Session-Registration</c> header onto the current response.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="principal">The principal the challenge is bound to. When <see langword="null"/> an empty principal is used.</param>
    /// <param name="dbscScheme">The resolved DBSC handler scheme name whose options drive the header.</param>
    public static void Emit(HttpContext httpContext, ClaimsPrincipal? principal, string dbscScheme)
    {
        // Dependencies are resolved from the request scope rather than constructor-injected: this is a
        // shared helper used by the inline OnSigningIn delegate (wired by the singleton
        // IPostConfigureOptions), the DI-less DeviceBoundSessionCookieEvents wrapper, and the public
        // HttpContext extension, none of which hold these dependencies. It runs per-request, so the
        // live HttpContext.RequestServices is the only source all callers share. (Both IOptionsMonitor<>
        // and the challenge protector are singletons, so request-scope resolution is for sharing, not
        // lifetime; the genuinely per-request input is the principal.)
        var dbscOptions = httpContext.RequestServices
            .GetRequiredService<IOptionsMonitor<DeviceBoundSessionOptions>>()
            .Get(dbscScheme);
        var challengeProtector = httpContext.RequestServices
            .GetRequiredService<DeviceBoundSessionChallengeProtector>();

        var effectivePrincipal = principal ?? new ClaimsPrincipal();
        var challenge = challengeProtector.GenerateRegistrationChallenge(effectivePrincipal, dbscOptions.ChallengeMaxAge);

        var headerValue = $"{DeviceBoundSessionConstants.AdvertisedAlgorithms};path=\"{dbscOptions.RegistrationPath.Value}\";challenge=\"{challenge}\"";
        httpContext.Response.Headers.Append(DeviceBoundSessionConstants.Headers.Registration, headerValue);
    }
}
