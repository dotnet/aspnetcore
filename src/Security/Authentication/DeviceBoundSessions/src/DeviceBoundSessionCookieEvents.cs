// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

internal sealed class PostConfigureDeviceBoundSessionCookieOptions : IPostConfigureOptions<CookieAuthenticationOptions>
{
    private readonly IOptions<DeviceBoundSessionSourceSchemes> _sourceSchemes;

    public PostConfigureDeviceBoundSessionCookieOptions(IOptions<DeviceBoundSessionSourceSchemes> sourceSchemes)
    {
        _sourceSchemes = sourceSchemes;
    }

    public void PostConfigure(string? name, CookieAuthenticationOptions options)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (!_sourceSchemes.Value.Schemes.TryGetValue(name, out var dbscScheme))
        {
            return;
        }

        if (options.EventsType is null)
        {
            var priorSigningIn = options.Events.OnSigningIn;
            options.Events.OnSigningIn = async context =>
            {
                EmitRegistrationHeader(context, dbscScheme);
                await priorSigningIn(context);
            };
            return;
        }

        options.Events = new DeviceBoundSessionCookieEvents(dbscScheme, options.Events, options.EventsType);
        options.EventsType = null;
    }

    internal static void EmitRegistrationHeader(CookieSigningInContext context, string dbscScheme)
    {
        var dbscOptions = context.HttpContext.RequestServices
            .GetRequiredService<IOptionsMonitor<DeviceBoundSessionOptions>>()
            .Get(dbscScheme);
        var dataProtectionProvider = context.HttpContext.RequestServices.GetRequiredService<IDataProtectionProvider>();

        var principal = context.Principal ?? new System.Security.Claims.ClaimsPrincipal();
        var challenge = DeviceBoundSessionChallengeProtector.GenerateChallenge(
            dataProtectionProvider,
            principal,
            DeviceBoundSessionChallengeProtector.RegistrationSessionId,
            dbscOptions.ChallengeMaxAge);

        var headerValue = $"(ES256 RS256);path=\"{dbscOptions.RegistrationPath.Value}\";challenge=\"{challenge}\"";
        context.Response.Headers.Append("Secure-Session-Registration", headerValue);
    }
}

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

internal sealed class DeviceBoundSessionSourceSchemes
{
    public IDictionary<string, string> Schemes { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
}
