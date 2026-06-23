// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

/// <summary>
/// Extension methods on <see cref="HttpContext"/> for Device Bound Session Credentials (DBSC).
/// </summary>
[Experimental("ASP0030", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public static class DeviceBoundSessionHttpContextExtensions
{
    /// <summary>
    /// Writes the DBSC <c>Secure-Session-Registration</c> response header for the current
    /// authenticated user (<see cref="HttpContext.User"/>), advertising the supported signing
    /// algorithms and a fresh, principal-bound registration challenge.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this to opt an <em>already signed-in</em> user into a device bound session without forcing a
    /// re-login &#8212; for example, to migrate existing sessions after deploying DBSC instead of waiting
    /// for them to expire. Registration is not tied to the sign-in event: a DBSC-capable browser reacts
    /// to this header on any authenticated response and starts registration, sending the proof to the
    /// registration endpoint with the existing source cookie attached. Browsers that do not support DBSC
    /// ignore the header, so it is safe to emit broadly.
    /// </para>
    /// <para>
    /// Calling this does not itself create a session; it only advertises registration. Emitting it
    /// repeatedly mints a new challenge each time, so the caller owns idempotency: gate the call so a
    /// user is offered registration at most once (for example, with a durable marker cookie), and skip
    /// clients that are already bound (detected by the presence of the bound session cookie). The header
    /// is only honored over HTTPS.
    /// </para>
    /// </remarks>
    /// <param name="context">The current <see cref="HttpContext"/>.</param>
    /// <param name="sourceScheme">
    /// The source cookie authentication scheme that was passed to
    /// <c>AddDeviceBoundSession</c> (for example <c>IdentityConstants.ApplicationScheme</c>).
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="sourceScheme"/> is <see langword="null"/> or empty.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="sourceScheme"/> is not a registered Device Bound Session source scheme, or the
    /// current user is not authenticated.
    /// </exception>
    /// <example>
    /// Migration middleware that offers DBSC registration to already-signed-in users once each. It skips
    /// clients that already have a bound session (the bound session cookie is present) and uses a durable
    /// marker cookie so the offer is not repeated on every request:
    /// <code>
    /// // The DBSC bound session cookie follows .AspNetCore.{sourceScheme}.Dbsc.Session
    /// const string boundCookie = ".AspNetCore.Identity.Application.Dbsc.Session";
    ///
    /// app.Use(async (context, next) =>
    /// {
    ///     var alreadyBound = context.Request.Cookies.ContainsKey(boundCookie);
    ///     var alreadyOffered = context.Request.Cookies.ContainsKey("dbsc-offered");
    ///
    ///     if (context.User.Identity?.IsAuthenticated == true &amp;&amp; !alreadyBound &amp;&amp; !alreadyOffered)
    ///     {
    ///         context.WriteDeviceBoundSessionRegistration(IdentityConstants.ApplicationScheme);
    ///
    ///         context.Response.Cookies.Append("dbsc-offered", "1", new CookieOptions
    ///         {
    ///             Path = "/",
    ///             Secure = true,
    ///             HttpOnly = true,
    ///             SameSite = SameSiteMode.Lax,
    ///         });
    ///     }
    ///
    ///     await next();
    /// });
    /// </code>
    /// </example>
    public static void WriteDeviceBoundSessionRegistration(this HttpContext context, string sourceScheme)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrEmpty(sourceScheme);

        var principal = context.User;
        if (principal.Identity?.IsAuthenticated != true)
        {
            throw new InvalidOperationException(
                "The Device Bound Session registration challenge must be bound to an authenticated principal.");
        }

        var sourceSchemes = context.RequestServices
            .GetRequiredService<IOptions<DeviceBoundSessionSourceSchemes>>().Value;
        if (!sourceSchemes.Schemes.TryGetValue(sourceScheme, out var dbscScheme))
        {
            throw new InvalidOperationException(
                $"'{sourceScheme}' is not registered as a Device Bound Session source scheme. " +
                $"Call AddDeviceBoundSession(\"{sourceScheme}\") to register it first.");
        }

        DeviceBoundSessionRegistrationHeader.Emit(context, principal, dbscScheme);
    }
}
