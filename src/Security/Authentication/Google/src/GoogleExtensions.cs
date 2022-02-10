// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to configure Google OAuth authentication.
/// </summary>
public static class GoogleExtensions
{
    /// <summary>
    /// Adds Google OAuth-based authentication to <see cref="AuthenticationBuilder"/> using the default scheme.
    /// The default scheme is specified by <see cref="GoogleDefaults.AuthenticationScheme"/>.
    /// <para>
    /// Google authentication allows application users to sign in with their Google account.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddGoogle(this AuthenticationBuilder builder)
        => builder.AddGoogle(GoogleDefaults.AuthenticationScheme, _ => { });

    /// <summary>
    /// Adds Google OAuth-based authentication to <see cref="AuthenticationBuilder"/> using the default scheme.
    /// The default scheme is specified by <see cref="GoogleDefaults.AuthenticationScheme"/>.
    /// <para>
    /// Google authentication allows application users to sign in with their Google account.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="GoogleOptions"/>.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddGoogle(this AuthenticationBuilder builder, Action<GoogleOptions> configureOptions)
        => builder.AddGoogle(GoogleDefaults.AuthenticationScheme, configureOptions);

    /// <summary>
    /// Adds Google OAuth-based authentication to <see cref="AuthenticationBuilder"/> using the default scheme.
    /// The default scheme is specified by <see cref="GoogleDefaults.AuthenticationScheme"/>.
    /// <para>
    /// Google authentication allows application users to sign in with their Google account.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="authenticationScheme">The authentication scheme.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="GoogleOptions"/>.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddGoogle(this AuthenticationBuilder builder, string authenticationScheme, Action<GoogleOptions> configureOptions)
        => builder.AddGoogle(authenticationScheme, GoogleDefaults.DisplayName, configureOptions);

    /// <summary>
    /// Adds Google OAuth-based authentication to <see cref="AuthenticationBuilder"/> using the default scheme.
    /// The default scheme is specified by <see cref="GoogleDefaults.AuthenticationScheme"/>.
    /// <para>
    /// Google authentication allows application users to sign in with their Google account.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="authenticationScheme">The authentication scheme.</param>
    /// <param name="displayName">A display name for the authentication handler.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="GoogleOptions"/>.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddGoogle(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<GoogleOptions> configureOptions)
        => builder.AddOAuth<GoogleOptions, GoogleHandler>(authenticationScheme, displayName, configureOptions);
}
