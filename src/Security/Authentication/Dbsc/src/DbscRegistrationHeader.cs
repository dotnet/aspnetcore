#pragma warning disable ASP0031 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Authentication.Dbsc;

/// <summary>
/// Writes the DBSC <c>Secure-Session-Registration</c> response header, advertising the supported
/// signing algorithms and a fresh registration challenge bound to a principal. Shared by the
/// source cookie sign-in triggers and by the public <see cref="DbscRegistration"/>
/// on-demand entry point.
/// </summary>
internal static class DbscRegistrationHeader
{
    /// <summary>
    /// Emits the <c>Secure-Session-Registration</c> header onto the current response.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="principal">The principal the challenge is bound to. When <see langword="null"/> an empty principal is used.</param>
    /// <param name="properties">The authentication properties associated with the source sign-in.</param>
    /// <param name="dbscScheme">The resolved DBSC handler scheme name whose options drive the header.</param>
    public static async Task Emit(HttpContext httpContext, ClaimsPrincipal? principal, AuthenticationProperties? properties, string dbscScheme)
    {
        // Dependencies are resolved from the request scope rather than constructor-injected: this is a
        // shared helper used by the inline OnSigningIn delegate (wired by the singleton
        // IPostConfigureOptions), the DI-less DbscCookieEvents wrapper, and the public
        // HttpContext extension, none of which hold these dependencies. It runs per-request, so the
        // live HttpContext.RequestServices is the only source all callers share. (Both IOptionsMonitor<>
        // and the challenge protector are singletons, so request-scope resolution is for sharing, not
        // lifetime; the genuinely per-request input is the principal.)
        var dbscOptions = httpContext.RequestServices
            .GetRequiredService<IOptionsMonitor<DbscOptions>>()
            .Get(dbscScheme);
        var challengeProtector = httpContext.RequestServices
            .GetRequiredService<DbscChallengeProtector>();

        var effectivePrincipal = principal ?? new ClaimsPrincipal();
        var scheme = await httpContext.RequestServices
            .GetRequiredService<IAuthenticationSchemeProvider>()
            .GetSchemeAsync(dbscScheme)
            ?? throw new InvalidOperationException($"No authentication scheme is registered for '{dbscScheme}'.");
        var events = dbscOptions.EventsType is null
            ? dbscOptions.Events
            : (DbscEvents)httpContext.RequestServices.GetRequiredService(dbscOptions.EventsType);
        var eventContext = new DbscRegistrationHeaderCreatingContext(
            httpContext,
            scheme,
            dbscOptions,
            effectivePrincipal,
            properties);
        await events.RegistrationHeaderCreating(eventContext);

        var challenge = challengeProtector.GenerateRegistrationChallenge(eventContext.Principal, dbscOptions.ChallengeMaxAge);

        // Advertise the registration endpoint relative to the application's path base so an app mounted
        // under a non-root path base (e.g. "/foo") tells the browser to POST to "/foo/.well-known/dbsc/..."
        // rather than the origin root.
        var registrationPath = httpContext.Request.PathBase.Add(dbscOptions.RegistrationPath).ToUriComponent();
        var headerValue = $"{DbscConstants.AdvertisedAlgorithms};path={HeaderUtilities.EscapeAsQuotedString(registrationPath)};challenge={HeaderUtilities.EscapeAsQuotedString(challenge)}";
        httpContext.Response.Headers.Append(DbscConstants.Headers.Registration, headerValue);
    }
}
