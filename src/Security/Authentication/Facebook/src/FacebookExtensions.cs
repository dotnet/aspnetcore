// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to configure Facebook OAuth authentication.
/// </summary>
public static class FacebookAuthenticationOptionsExtensions
{
    /// <summary>
    /// Adds Facebook OAuth-based authentication to <see cref="AuthenticationBuilder"/> using the default scheme.
    /// The default scheme is specified by <see cref="FacebookDefaults.AuthenticationScheme"/>.
    /// <para>
    /// Facebook authentication allows application users to sign in with their Facebook account.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddFacebook(this AuthenticationBuilder builder)
        => builder.AddFacebook(FacebookDefaults.AuthenticationScheme, _ => { });

    /// <summary>
    /// Adds Facebook OAuth-based authentication to <see cref="AuthenticationBuilder"/> using the default scheme.
    /// The default scheme is specified by <see cref="FacebookDefaults.AuthenticationScheme"/>.
    /// <para>
    /// Facebook authentication allows application users to sign in with their Facebook account.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="FacebookOptions"/>.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddFacebook(this AuthenticationBuilder builder, Action<FacebookOptions> configureOptions)
        => builder.AddFacebook(FacebookDefaults.AuthenticationScheme, configureOptions);

    /// <summary>
    /// Adds Facebook OAuth-based authentication to <see cref="AuthenticationBuilder"/> using the default scheme.
    /// The default scheme is specified by <see cref="FacebookDefaults.AuthenticationScheme"/>.
    /// <para>
    /// Facebook authentication allows application users to sign in with their Facebook account.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="authenticationScheme">The authentication scheme.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="FacebookOptions"/>.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddFacebook(this AuthenticationBuilder builder, string authenticationScheme, Action<FacebookOptions> configureOptions)
        => builder.AddFacebook(authenticationScheme, FacebookDefaults.DisplayName, configureOptions);

    /// <summary>
    /// Adds Facebook OAuth-based authentication to <see cref="AuthenticationBuilder"/> using the default scheme.
    /// The default scheme is specified by <see cref="FacebookDefaults.AuthenticationScheme"/>.
    /// <para>
    /// Facebook authentication allows application users to sign in with their Facebook account.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="authenticationScheme">The authentication scheme.</param>
    /// <param name="displayName">A display name for the authentication handler.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="FacebookOptions"/>.</param>
    public static AuthenticationBuilder AddFacebook(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<FacebookOptions> configureOptions)
        => builder.AddOAuth<FacebookOptions, FacebookHandler>(authenticationScheme, displayName, configureOptions);
}
