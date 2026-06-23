// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

/// <summary>
/// Post-configures each cookie scheme that is a DBSC source scheme so that the
/// <c>Secure-Session-Registration</c> header is emitted during sign-in. For delegate-based events it
/// chains <see cref="CookieAuthenticationEvents.OnSigningIn"/>; for <c>EventsType</c> scenarios it
/// wraps the events with <see cref="DeviceBoundSessionCookieEvents"/>.
/// </summary>
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
                DeviceBoundSessionRegistrationHeader.Emit(context.HttpContext, context.Principal, dbscScheme);
                await priorSigningIn(context);
            };
            return;
        }

        options.Events = new DeviceBoundSessionCookieEvents(dbscScheme, options.Events, options.EventsType);
        options.EventsType = null;
    }
}
