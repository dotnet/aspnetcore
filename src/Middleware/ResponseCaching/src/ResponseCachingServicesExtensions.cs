// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.ResponseCaching;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for the ResponseCaching middleware.
/// </summary>
public static class ResponseCachingServicesExtensions
{
    /// <summary>
    /// Add response caching services.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <returns></returns>
    public static IServiceCollection AddResponseCaching(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();

        return services;
    }

    /// <summary>
    /// Add response caching services and configure the related options.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <param name="configureOptions">A delegate to configure the <see cref="ResponseCachingOptions"/>.</param>
    /// <returns></returns>
    public static IServiceCollection AddResponseCaching(this IServiceCollection services, Action<ResponseCachingOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.Configure(configureOptions);
        services.AddResponseCaching();

        return services;
    }
}
