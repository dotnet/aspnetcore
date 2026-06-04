// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.DeviceBoundSessions;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using DbscOptions = Microsoft.AspNetCore.Authentication.DeviceBoundSessions.DeviceBoundSessionOptions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to configure Device Bound Session Credentials authentication.
/// </summary>
public static class DeviceBoundSessionExtensions
{
    /// <summary>
    /// Adds Device Bound Session Credentials (DBSC) authentication that wraps an existing cookie scheme.
    /// This sets up: a policy scheme (default auth), a refresh cookie scheme (path-scoped stash),
    /// a session cookie scheme (short-lived), and the DBSC protocol handler.
    /// </summary>
    /// <param name="builder">The authentication builder.</param>
    /// <param name="sourceScheme">The existing cookie authentication scheme to protect with DBSC (e.g., "Identity.Application").</param>
    /// <param name="configureOptions">Optional action to configure DBSC options.</param>
    /// <returns>The authentication builder for chaining.</returns>
    public static AuthenticationBuilder AddDeviceBoundSession(
        this AuthenticationBuilder builder,
        string sourceScheme,
        Action<DbscOptions>? configureOptions = null)
    {
        return AddDeviceBoundSession(builder, DeviceBoundSessionDefaults.AuthenticationScheme, sourceScheme, configureOptions);
    }

    /// <summary>
    /// Adds Device Bound Session Credentials (DBSC) authentication that wraps an existing cookie scheme.
    /// </summary>
    /// <param name="builder">The authentication builder.</param>
    /// <param name="authenticationScheme">The DBSC authentication scheme name.</param>
    /// <param name="sourceScheme">The existing cookie authentication scheme to protect with DBSC.</param>
    /// <param name="configureOptions">Optional action to configure DBSC options.</param>
    /// <returns>The authentication builder for chaining.</returns>
    public static AuthenticationBuilder AddDeviceBoundSession(
        this AuthenticationBuilder builder,
        string authenticationScheme,
        string sourceScheme,
        Action<DbscOptions>? configureOptions = null)
    {
        var refreshScheme = $"{sourceScheme}.Dbsc.Refresh";
        var sessionScheme = $"{sourceScheme}.Dbsc.Session";
        var policyScheme = $"{sourceScheme}.Dbsc";

        // Add the refresh cookie scheme — settings will be copied from the source
        // scheme via PostConfigureDeviceBoundSessionDerivedCookieOptions
        builder.AddCookie(refreshScheme, o =>
        {
            o.Cookie.Name = $".AspNetCore.{sourceScheme}.Dbsc.Refresh";
            o.Cookie.Path = "/.well-known/dbsc";
        });

        // Add the session cookie scheme — settings copied from source, expiry overridden
        builder.AddCookie(sessionScheme, o =>
        {
            o.Cookie.Name = $".AspNetCore.{sourceScheme}.Dbsc.Session";
        });

        // Add a policy scheme that tries the session cookie first, then falls back to the source scheme
        var sessionCookieName = $".AspNetCore.{sourceScheme}.Dbsc.Session";
        builder.AddPolicyScheme(policyScheme, policyScheme, o =>
        {
            o.ForwardDefaultSelector = context =>
            {
                if (context.Request.Cookies.ContainsKey(sessionCookieName))
                {
                    return sessionScheme;
                }
                return sourceScheme;
            };
        });

        // Register services
        builder.Services.TryAddSingleton<DeviceBoundSessionChallengeProtector>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<CookieAuthenticationOptions>, PostConfigureDeviceBoundSessionCookieOptions>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<CookieAuthenticationOptions>, PostConfigureDeviceBoundSessionDerivedCookieOptions>());
        builder.Services.Configure<DeviceBoundSessionSourceSchemes>(o =>
        {
            o.Schemes[sourceScheme] = authenticationScheme;
            o.RefreshSchemes[refreshScheme] = sourceScheme;
            o.SessionSchemes[sessionScheme] = sourceScheme;
        });

        // Add the DBSC protocol handler
        builder.AddScheme<DbscOptions, DeviceBoundSessionHandler>(authenticationScheme, o =>
        {
            o.RegistrationSourceScheme = sourceScheme;
            o.RefreshScheme = refreshScheme;
            o.SessionScheme = sessionScheme;
            configureOptions?.Invoke(o);
        });

        // Set the policy scheme as the default authenticate scheme
        builder.Services.Configure<AuthenticationOptions>(o =>
        {
            o.DefaultAuthenticateScheme = policyScheme;
            o.DefaultSignInScheme = sourceScheme;
        });

        return builder;
    }
}
