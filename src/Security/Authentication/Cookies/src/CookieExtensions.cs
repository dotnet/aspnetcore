// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to configure cookie authentication.
/// </summary>
public static class CookieExtensions
{
    /// <summary>
    /// Adds cookie authentication to <see cref="AuthenticationBuilder"/> using the default scheme.
    /// The default scheme is specified by <see cref="CookieAuthenticationDefaults.AuthenticationScheme"/>.
    /// <para>
    /// Cookie authentication uses a HTTP cookie persisted in the client to perform authentication.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddCookie(this AuthenticationBuilder builder)
        => builder.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);

    /// <summary>
    /// Adds cookie authentication to <see cref="AuthenticationBuilder"/> using the specified scheme.
    /// <para>
    /// Cookie authentication uses a HTTP cookie persisted in the client to perform authentication.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="authenticationScheme">The authentication scheme.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddCookie(this AuthenticationBuilder builder, string authenticationScheme)
        => builder.AddCookie(authenticationScheme, configureOptions: null!);

    /// <summary>
    /// Adds cookie authentication to <see cref="AuthenticationBuilder"/> using the default scheme.
    /// The default scheme is specified by <see cref="CookieAuthenticationDefaults.AuthenticationScheme"/>.
    /// <para>
    /// Cookie authentication uses a HTTP cookie persisted in the client to perform authentication.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="CookieAuthenticationOptions"/>.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddCookie(this AuthenticationBuilder builder, Action<CookieAuthenticationOptions> configureOptions)
        => builder.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, configureOptions);

    /// <summary>
    /// Adds cookie authentication to <see cref="AuthenticationBuilder"/> using the specified scheme.
    /// <para>
    /// Cookie authentication uses a HTTP cookie persisted in the client to perform authentication.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="authenticationScheme">The authentication scheme.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="CookieAuthenticationOptions"/>.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddCookie(this AuthenticationBuilder builder, string authenticationScheme, Action<CookieAuthenticationOptions> configureOptions)
        => builder.AddCookie(authenticationScheme, displayName: null, configureOptions: configureOptions);

    /// <summary>
    /// Adds cookie authentication to <see cref="AuthenticationBuilder"/> using the specified scheme.
    /// <para>
    /// Cookie authentication uses a HTTP cookie persisted in the client to perform authentication.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="authenticationScheme">The authentication scheme.</param>
    /// <param name="displayName">A display name for the authentication handler.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="CookieAuthenticationOptions"/>.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddCookie(this AuthenticationBuilder builder, string authenticationScheme, string? displayName, Action<CookieAuthenticationOptions> configureOptions)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<CookieAuthenticationOptions>, PostConfigureCookieAuthenticationOptions>());
        builder.Services.AddOptions<CookieAuthenticationOptions>(authenticationScheme).Validate(o => o.Cookie.Expiration == null, "Cookie.Expiration is ignored, use ExpireTimeSpan instead.");
        return builder.AddScheme<CookieAuthenticationOptions, CookieAuthenticationHandler>(authenticationScheme, displayName, configureOptions);
    }
}
