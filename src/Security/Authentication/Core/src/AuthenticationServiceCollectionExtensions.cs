// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up authentication services in an <see cref="IServiceCollection" />.
/// </summary>
public static class AuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// Registers services required by authentication services.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>A <see cref="AuthenticationBuilder"/> that can be used to further configure authentication.</returns>
    public static AuthenticationBuilder AddAuthentication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthenticationCore();
        services.AddDataProtection();
        services.AddWebEncoders();
        services.TryAddSingleton(TimeProvider.System);
#pragma warning disable CS0618 // Type or member is obsolete
        services.TryAddSingleton<ISystemClock, SystemClock>();
#pragma warning restore CS0618 // Type or member is obsolete
        services.TryAddSingleton<IAuthenticationConfigurationProvider, DefaultAuthenticationConfigurationProvider>();

        return new AuthenticationBuilder(services);
    }

    /// <summary>
    /// Registers services required by authentication services. <paramref name="defaultScheme"/> specifies the name of the
    /// scheme to use by default when a specific scheme isn't requested.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="defaultScheme">The default scheme used as a fallback for all other schemes.</param>
    /// <returns>A <see cref="AuthenticationBuilder"/> that can be used to further configure authentication.</returns>
    public static AuthenticationBuilder AddAuthentication(this IServiceCollection services, string defaultScheme)
        => services.AddAuthentication(o => o.DefaultScheme = defaultScheme);

    /// <summary>
    /// Registers services required by authentication services and configures <see cref="AuthenticationOptions"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="AuthenticationOptions"/>.</param>
    /// <returns>A <see cref="AuthenticationBuilder"/> that can be used to further configure authentication.</returns>
    public static AuthenticationBuilder AddAuthentication(this IServiceCollection services, Action<AuthenticationOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var builder = services.AddAuthentication();
        services.Configure(configureOptions);
        return builder;
    }
}
