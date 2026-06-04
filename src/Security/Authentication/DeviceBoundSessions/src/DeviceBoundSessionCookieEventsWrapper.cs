// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

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
        PostConfigureDeviceBoundSessionCookieOptions.EmitRegistrationHeader(context, _dbscScheme);
        await GetInnerEvents(context.HttpContext).SigningIn(context);
    }

    public override async Task SignedIn(CookieSignedInContext context)
    {
        await GetInnerEvents(context.HttpContext).SignedIn(context);
    }

    public override async Task SigningOut(CookieSigningOutContext context)
    {
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
}
