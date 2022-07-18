// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to configure OpenIdConnect authentication.
/// </summary>
public static class OpenIdConnectExtensions
{
    /// <summary>
    /// Adds OpenId Connect authentication to <see cref="AuthenticationBuilder"/> using the default scheme.
    /// The default scheme is specified by <see cref="OpenIdConnectDefaults.AuthenticationScheme"/>.
    /// <para>
    /// OpenID Connect is an identity layer on top of the OAuth 2.0 protocol. It allows clients
    /// to request and receive information about authenticated sessions and end-users.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddOpenIdConnect(this AuthenticationBuilder builder)
        => builder.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, _ => { });

    /// <summary>
    /// Adds OpenId Connect authentication to <see cref="AuthenticationBuilder"/> using the default scheme.
    /// The default scheme is specified by <see cref="OpenIdConnectDefaults.AuthenticationScheme"/>.
    /// <para>
    /// OpenID Connect is an identity layer on top of the OAuth 2.0 protocol. It allows clients
    /// to request and receive information about authenticated sessions and end-users.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="OpenIdConnectOptions"/>.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddOpenIdConnect(this AuthenticationBuilder builder, Action<OpenIdConnectOptions> configureOptions)
        => builder.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, configureOptions);

    /// <summary>
    /// Adds OpenId Connect authentication to <see cref="AuthenticationBuilder"/> using the specified scheme.
    /// <para>
    /// OpenID Connect is an identity layer on top of the OAuth 2.0 protocol. It allows clients
    /// to request and receive information about authenticated sessions and end-users.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="authenticationScheme">The authentication scheme.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="OpenIdConnectOptions"/>.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddOpenIdConnect(this AuthenticationBuilder builder, string authenticationScheme, Action<OpenIdConnectOptions> configureOptions)
        => builder.AddOpenIdConnect(authenticationScheme, OpenIdConnectDefaults.DisplayName, configureOptions);

    /// <summary>
    /// Adds OpenId Connect authentication to <see cref="AuthenticationBuilder"/> using the specified scheme.
    /// <para>
    /// OpenID Connect is an identity layer on top of the OAuth 2.0 protocol. It allows clients
    /// to request and receive information about authenticated sessions and end-users.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="authenticationScheme">The authentication scheme.</param>
    /// <param name="displayName">A display name for the authentication handler.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="OpenIdConnectOptions"/>.</param>
    /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
    public static AuthenticationBuilder AddOpenIdConnect(this AuthenticationBuilder builder, string authenticationScheme, string? displayName, Action<OpenIdConnectOptions> configureOptions)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<OpenIdConnectOptions>, OpenIdConnectConfigureOptions>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<OpenIdConnectOptions>, OpenIdConnectPostConfigureOptions>());
        return builder.AddRemoteScheme<OpenIdConnectOptions, OpenIdConnectHandler>(authenticationScheme, displayName, configureOptions);
    }
}
