// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up memory cache related services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class MemoryCacheServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a non distributed in memory implementation of <see cref="IMemoryCache"/> to the
        /// <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddMemoryCache(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions();
            services.TryAdd(ServiceDescriptor.Singleton<IMemoryCache, MemoryCache>());

            return services;
        }

        /// <summary>
        /// Adds a non distributed in memory implementation of <see cref="IMemoryCache"/> to the
        /// <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="setupAction">
        /// The <see cref="Action{MemoryCacheOptions}"/> to configure the provided <see cref="MemoryCacheOptions"/>.
        /// </param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddMemoryCache(this IServiceCollection services, Action<MemoryCacheOptions> setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            services.AddMemoryCache();
            services.Configure(setupAction);

            return services;
        }

        /// <summary>
        /// Adds a default implementation of <see cref="IDistributedCache"/> that stores items in memory
        /// to the <see cref="IServiceCollection" />. Frameworks that require a distributed cache to work
        /// can safely add this dependency as part of their dependency list to ensure that there is at least
        /// one implementation available.
        /// </summary>
        /// <remarks>
        /// <see cref="AddDistributedMemoryCache(IServiceCollection)"/> should only be used in single
        /// server scenarios as this cache stores items in memory and doesn't expand across multiple machines.
        /// For those scenarios it is recommended to use a proper distributed cache that can expand across
        /// multiple machines.
        /// </remarks>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddDistributedMemoryCache(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions();
            services.TryAdd(ServiceDescriptor.Singleton<IDistributedCache, MemoryDistributedCache>());

            return services;
        }

        /// <summary>
        /// Adds a default implementation of <see cref="IDistributedCache"/> that stores items in memory
        /// to the <see cref="IServiceCollection" />. Frameworks that require a distributed cache to work
        /// can safely add this dependency as part of their dependency list to ensure that there is at least
        /// one implementation available.
        /// </summary>
        /// <remarks>
        /// <see cref="AddDistributedMemoryCache(IServiceCollection)"/> should only be used in single
        /// server scenarios as this cache stores items in memory and doesn't expand across multiple machines.
        /// For those scenarios it is recommended to use a proper distributed cache that can expand across
        /// multiple machines.
        /// </remarks>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="setupAction">
        /// The <see cref="Action{MemoryDistributedCacheOptions}"/> to configure the provided <see cref="MemoryDistributedCacheOptions"/>.
        /// </param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddDistributedMemoryCache(this IServiceCollection services, Action<MemoryDistributedCacheOptions> setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            services.AddDistributedMemoryCache();
            services.Configure(setupAction);

            return services;
        }
    }
}
