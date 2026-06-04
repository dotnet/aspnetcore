// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.DeviceBoundSessions;
using Microsoft.AspNetCore.Http;
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

        // Add the refresh cookie scheme (path-scoped to /.well-known/dbsc/)
        builder.AddCookie(refreshScheme, o =>
        {
            o.Cookie.Name = $".AspNetCore.{sourceScheme}.Dbsc.Refresh";
            o.Cookie.Path = "/.well-known/dbsc";
            o.Cookie.HttpOnly = true;
            o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            o.Cookie.SameSite = SameSiteMode.Lax;
            // Long-lived — matches original session lifetime
            o.ExpireTimeSpan = TimeSpan.FromDays(7);
            o.SlidingExpiration = false;
        });

        // Add the session cookie scheme (short-lived, path=/)
        builder.AddCookie(sessionScheme, o =>
        {
            o.Cookie.Name = $".AspNetCore.{sourceScheme}.Dbsc.Session";
            o.Cookie.Path = "/";
            o.Cookie.HttpOnly = true;
            o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            o.Cookie.SameSite = SameSiteMode.Lax;
            o.ExpireTimeSpan = TimeSpan.FromSeconds(30);
            o.SlidingExpiration = false;
        });

        // Add a policy scheme that tries the session cookie first, then falls back to the source scheme
        var sessionCookieName = $".AspNetCore.{sourceScheme}.Dbsc.Session";
        builder.AddPolicyScheme(policyScheme, policyScheme, o =>
        {
            o.ForwardDefaultSelector = context =>
            {
                // If the short-lived session cookie exists, use it
                if (context.Request.Cookies.ContainsKey(sessionCookieName))
                {
                    return sessionScheme;
                }
                // Otherwise fall back to the source cookie (pre-registration or non-DBSC browser)
                return sourceScheme;
            };
        });

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<CookieAuthenticationOptions>, PostConfigureDeviceBoundSessionCookieOptions>());
        builder.Services.Configure<DeviceBoundSessionSourceSchemes>(o => o.Schemes[sourceScheme] = authenticationScheme);

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
