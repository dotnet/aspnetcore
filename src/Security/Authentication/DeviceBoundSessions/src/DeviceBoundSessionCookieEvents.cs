// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0031 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

/// <summary>
/// A <see cref="CookieAuthenticationEvents"/> decorator used when a DBSC source cookie scheme sets
/// the cookie options' <c>EventsType</c>. It emits the DBSC registration header on
/// sign-in and forwards every event to the application's configured (DI-resolved) inner events.
/// </summary>
internal sealed class DeviceBoundSessionCookieEvents : CookieAuthenticationEvents
{
    private readonly string _dbscScheme;
    private readonly CookieAuthenticationEvents _innerEvents;
    private readonly Type? _innerEventsType;

    public DeviceBoundSessionCookieEvents(string dbscScheme, CookieAuthenticationEvents innerEvents, Type? innerEventsType)
    {
        _dbscScheme = dbscScheme;
        _innerEvents = innerEvents;
        _innerEventsType = innerEventsType;
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        await GetInnerEvents(context.HttpContext).ValidatePrincipal(context);
    }

    public override async Task CheckSlidingExpiration(CookieSlidingExpirationContext context)
    {
        await GetInnerEvents(context.HttpContext).CheckSlidingExpiration(context);
    }

    public override async Task SigningIn(CookieSigningInContext context)
    {
        await GetInnerEvents(context.HttpContext).SigningIn(context);
        DeviceBoundSessionRegistrationHeader.Emit(context.HttpContext, context.Principal, _dbscScheme);
    }

    public override async Task SignedIn(CookieSignedInContext context)
    {
        await GetInnerEvents(context.HttpContext).SignedIn(context);
    }

    public override async Task SigningOut(CookieSigningOutContext context)
    {
        await ClearDerivedCookiesAsync(context.HttpContext, _dbscScheme);
        await GetInnerEvents(context.HttpContext).SigningOut(context);
    }

    public override async Task RedirectToLogout(RedirectContext<CookieAuthenticationOptions> context)
    {
        await GetInnerEvents(context.HttpContext).RedirectToLogout(context);
    }

    public override async Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        await GetInnerEvents(context.HttpContext).RedirectToLogin(context);
    }

    public override async Task RedirectToReturnUrl(RedirectContext<CookieAuthenticationOptions> context)
    {
        await GetInnerEvents(context.HttpContext).RedirectToReturnUrl(context);
    }

    public override async Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        await GetInnerEvents(context.HttpContext).RedirectToAccessDenied(context);
    }

    private CookieAuthenticationEvents GetInnerEvents(HttpContext httpContext)
    {
        if (_innerEventsType is null)
        {
            return _innerEvents;
        }

        return (CookieAuthenticationEvents)httpContext.RequestServices.GetRequiredService(_innerEventsType);
    }

    /// <summary>
    /// Marker set on <see cref="HttpContext.Items"/> during the DBSC registration cookie exchange, when
    /// the handler signs out the source scheme to drop the long-lived cookie. It tells the source
    /// scheme's sign-out hook that this is an exchange, not a user logout, so it leaves the
    /// freshly-minted session and refresh cookies intact.
    /// </summary>
    internal const string RegistrationExchangeItemKey = "__DeviceBoundSession.RegistrationExchange";

    /// <summary>
    /// Clears the DBSC-derived session and refresh cookies when the source scheme signs out, so a plain
    /// <c>SignOutAsync(sourceScheme)</c> fully logs the user out. The application never needs to know the
    /// derived scheme names to clear them — just as it never needs them to create them. Shared with the
    /// delegate-based sign-out wiring installed by <see cref="PostConfigureDeviceBoundSessionCookieOptions"/>.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="dbscScheme">The resolved DBSC handler scheme whose options name the derived schemes.</param>
    internal static async Task ClearDerivedCookiesAsync(HttpContext httpContext, string dbscScheme)
    {
        // During DBSC registration the handler signs out the source scheme to complete the cookie
        // exchange. That is not a user logout, so leave the session/refresh cookies just minted intact.
        if (httpContext.Items.ContainsKey(RegistrationExchangeItemKey))
        {
            return;
        }

        var options = httpContext.RequestServices
            .GetRequiredService<IOptionsMonitor<DeviceBoundSessionOptions>>()
            .Get(dbscScheme);

        if (options.SessionScheme is not null)
        {
            await httpContext.SignOutAsync(options.SessionScheme);
        }

        if (options.RefreshScheme is not null)
        {
            await httpContext.SignOutAsync(options.RefreshScheme);
        }
    }
}
