// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0031 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.Dbsc;

/// <summary>
/// Implements the explicit integration contract with each DBSC source cookie scheme. The source scheme's
/// sign-in event is kept as a minimal trigger for transparent DBSC registration, while DBSC-owned events
/// provide the application extension point. Sign-out remains decorated to clear derived DBSC cookies.
/// </summary>
internal sealed class PostConfigureDbscCookieOptions : IPostConfigureOptions<CookieAuthenticationOptions>
{
    private readonly IOptions<DbscSourceSchemes> _sourceSchemes;
    private readonly IOptionsMonitor<DbscOptions> _dbscOptions;

    public PostConfigureDbscCookieOptions(
        IOptions<DbscSourceSchemes> sourceSchemes,
        IOptionsMonitor<DbscOptions> dbscOptions)
    {
        _sourceSchemes = sourceSchemes;
        _dbscOptions = dbscOptions;
    }

    public void PostConfigure(string? name, CookieAuthenticationOptions options)
    {
        ArgumentNullException.ThrowIfNull(name);

        var dbscScheme = _sourceSchemes.Value.FindDbscScheme(name, _dbscOptions);
        if (dbscScheme is null)
        {
            return;
        }

        if (options.EventsType is null)
        {
            var priorSigningIn = options.Events.OnSigningIn;
            options.Events.OnSigningIn = async context =>
            {
                await priorSigningIn(context);
                await DbscRegistrationHeader.Emit(context.HttpContext, context.Principal, context.Properties, dbscScheme);
            };

            var priorSigningOut = options.Events.OnSigningOut;
            options.Events.OnSigningOut = async context =>
            {
                await DbscCookieEvents.ClearDerivedCookiesAsync(context.HttpContext, dbscScheme);
                await priorSigningOut(context);
            };
            return;
        }

        options.Events = new DbscCookieEvents(dbscScheme, options.Events, options.EventsType);
        options.EventsType = null;
    }
}
