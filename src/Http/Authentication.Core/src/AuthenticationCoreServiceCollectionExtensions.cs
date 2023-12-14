// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up authentication services in an <see cref="IServiceCollection" />.
/// </summary>
public static class AuthenticationCoreServiceCollectionExtensions
{
    /// <summary>
    /// Add core authentication services needed for <see cref="IAuthenticationService"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddAuthenticationCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IAuthenticationService, AuthenticationService>();
        services.TryAddSingleton<IClaimsTransformation, NoopClaimsTransformation>(); // Can be replaced with scoped ones that use DbContext
        services.TryAddScoped<IAuthenticationHandlerProvider, AuthenticationHandlerProvider>();
        services.TryAddSingleton<IAuthenticationSchemeProvider, AuthenticationSchemeProvider>();
        return services;
    }

    /// <summary>
    /// Add core authentication services needed for <see cref="IAuthenticationService"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configureOptions">Used to configure the <see cref="AuthenticationOptions"/>.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddAuthenticationCore(this IServiceCollection services, Action<AuthenticationOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddAuthenticationCore();
        services.Configure(configureOptions);
        return services;
    }
}
