// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.WsFederation;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for registering the <see cref="WsFederationHandler"/>.
/// </summary>
public static class WsFederationExtensions
{
    /// <summary>
    /// Registers the <see cref="WsFederationHandler"/> using the default authentication scheme, display name, and options.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static AuthenticationBuilder AddWsFederation(this AuthenticationBuilder builder)
        => builder.AddWsFederation(WsFederationDefaults.AuthenticationScheme, _ => { });

    /// <summary>
    /// Registers the <see cref="WsFederationHandler"/> using the default authentication scheme, display name, and the given options configuration.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configureOptions">A delegate that configures the <see cref="WsFederationOptions"/>.</param>
    /// <returns></returns>
    public static AuthenticationBuilder AddWsFederation(this AuthenticationBuilder builder, Action<WsFederationOptions> configureOptions)
        => builder.AddWsFederation(WsFederationDefaults.AuthenticationScheme, configureOptions);

    /// <summary>
    /// Registers the <see cref="WsFederationHandler"/> using the given authentication scheme, default display name, and the given options configuration.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="authenticationScheme"></param>
    /// <param name="configureOptions">A delegate that configures the <see cref="WsFederationOptions"/>.</param>
    /// <returns></returns>
    public static AuthenticationBuilder AddWsFederation(this AuthenticationBuilder builder, string authenticationScheme, Action<WsFederationOptions> configureOptions)
        => builder.AddWsFederation(authenticationScheme, WsFederationDefaults.DisplayName, configureOptions);

    /// <summary>
    /// Registers the <see cref="WsFederationHandler"/> using the given authentication scheme, display name, and options configuration.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="authenticationScheme"></param>
    /// <param name="displayName"></param>
    /// <param name="configureOptions">A delegate that configures the <see cref="WsFederationOptions"/>.</param>
    /// <returns></returns>
    public static AuthenticationBuilder AddWsFederation(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<WsFederationOptions> configureOptions)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<WsFederationOptions>, WsFederationPostConfigureOptions>());
        return builder.AddRemoteScheme<WsFederationOptions, WsFederationHandler>(authenticationScheme, displayName, configureOptions);
    }
}
