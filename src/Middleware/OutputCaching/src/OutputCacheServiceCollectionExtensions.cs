// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.OutputCaching.Memory;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for the OutputCaching middleware.
/// </summary>
public static class OutputCacheServiceCollectionExtensions
{
    /// <summary>
    /// Add output caching services.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <returns></returns>
    public static IServiceCollection AddOutputCache(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTransient<IConfigureOptions<OutputCacheOptions>, OutputCacheOptionsSetup>();

        services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();

        services.TryAddSingleton<IOutputCacheStore>(sp =>
        {
            var outputCacheOptions = sp.GetRequiredService<IOptions<OutputCacheOptions>>();
            return new MemoryOutputCacheStore(new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = outputCacheOptions.Value.SizeLimit
            }));
        });
        return services;
    }

    /// <summary>
    /// Add output caching services and configure the related options.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <param name="configureOptions">A delegate to configure the <see cref="OutputCacheOptions"/>.</param>
    /// <returns></returns>
    public static IServiceCollection AddOutputCache(this IServiceCollection services, Action<OutputCacheOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddOutputCache();
        services.Configure(configureOptions);

        return services;
    }
}
