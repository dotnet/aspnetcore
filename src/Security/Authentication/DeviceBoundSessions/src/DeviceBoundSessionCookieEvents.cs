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
        var challenge = DeviceBoundSessionChallengeProtector.GenerateRegistrationChallenge(
            dataProtectionProvider,
            principal,
            dbscOptions.ChallengeMaxAge);

        var headerValue = $"(ES256 RS256);path=\"{dbscOptions.RegistrationPath.Value}\";challenge=\"{challenge}\"";
        context.Response.Headers.Append("Secure-Session-Registration", headerValue);
    }
}
