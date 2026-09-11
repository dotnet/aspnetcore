// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Dbsc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using DbscOptions = Microsoft.AspNetCore.Authentication.Dbsc.DbscOptions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to configure Device Bound Session Credentials authentication.
/// </summary>
[Experimental("ASP0031", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public static class DbscExtensions
{
    /// <summary>
    /// Adds Device Bound Session Credentials (DBSC) authentication using the default DBSC scheme.
    /// This sets up a refresh cookie scheme (path-scoped stash), a session cookie scheme (short-lived),
    /// and the DBSC protocol handler.
    /// </summary>
    /// <param name="builder">The authentication builder.</param>
    /// <returns>The authentication builder for chaining.</returns>
    public static AuthenticationBuilder AddDbsc(this AuthenticationBuilder builder)
        => AddDbscCore(builder, DbscDefaults.AuthenticationScheme, configureOptions: null);

    /// <summary>
    /// Adds Device Bound Session Credentials (DBSC) authentication using the specified DBSC scheme.
    /// </summary>
    /// <param name="builder">The authentication builder.</param>
    /// <param name="authenticationScheme">The DBSC authentication scheme name.</param>
    /// <returns>The authentication builder for chaining.</returns>
    public static AuthenticationBuilder AddDbsc(
        this AuthenticationBuilder builder,
        string authenticationScheme)
        => AddDbscCore(builder, authenticationScheme, configureOptions: null);

    /// <summary>
    /// Adds Device Bound Session Credentials (DBSC) authentication using the specified DBSC scheme.
    /// </summary>
    /// <param name="builder">The authentication builder.</param>
    /// <param name="authenticationScheme">The DBSC authentication scheme name.</param>
    /// <param name="configureOptions">Action to configure DBSC options, including the source authentication scheme.</param>
    /// <returns>The authentication builder for chaining.</returns>
    public static AuthenticationBuilder AddDbsc(
        this AuthenticationBuilder builder,
        string authenticationScheme,
        Action<DbscOptions> configureOptions)
        => AddDbscCore(builder, authenticationScheme, configureOptions);

    private static AuthenticationBuilder AddDbscCore(
        AuthenticationBuilder builder,
        string authenticationScheme,
        Action<DbscOptions>? configureOptions)
    {
        var refreshScheme = $"{authenticationScheme}.Refresh";
        var sessionScheme = $"{authenticationScheme}.Session";
        var escapedScheme = Uri.EscapeDataString(authenticationScheme);

        // Add the refresh cookie scheme — settings will be copied from the source
        // scheme via PostConfigureDbscDerivedCookieOptions
        builder.AddCookie(refreshScheme, o =>
        {
            o.Cookie.Name = $".AspNetCore.{escapedScheme}.Refresh";
            o.Cookie.Path = "/.well-known/dbsc";
        });

        // Add the session cookie scheme — settings copied from source, expiry overridden
        builder.AddCookie(sessionScheme, o =>
        {
            o.Cookie.Name = $".AspNetCore.{escapedScheme}.Session";
        });

        // Register services
        builder.Services.TryAddSingleton<DbscChallengeProtector>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<CookieAuthenticationOptions>, PostConfigureDbscCookieOptions>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<CookieAuthenticationOptions>, PostConfigureDbscDerivedCookieOptions>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<AuthenticationOptions>, PostConfigureDbscAuthenticationOptions>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<DbscOptions>, PostConfigureDbscOptions>());
        builder.Services.Configure<DbscSourceSchemes>(o =>
        {
            o.DbscSchemes.Add(authenticationScheme);
            o.RefreshSchemes[refreshScheme] = authenticationScheme;
            o.SessionSchemes[sessionScheme] = authenticationScheme;
        });

        // Add the DBSC protocol handler
        return builder.AddScheme<DbscOptions, DbscHandler>(authenticationScheme, o =>
        {
            o.RefreshScheme = refreshScheme;
            o.SessionScheme = sessionScheme;
            configureOptions?.Invoke(o);
        });
    }
}
