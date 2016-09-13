// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.ResponseCaching;
using Microsoft.AspNetCore.ResponseCaching.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ResponseCachingServiceCollectionExtensions
    {
        public static IServiceCollection AddMemoryResponseCache(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddMemoryCache();
            services.AddResponseCachingServices();
            services.TryAdd(ServiceDescriptor.Singleton<IResponseCache, MemoryResponseCache>());

            return services;
        }

        public static IServiceCollection AddDistributedResponseCache(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddDistributedMemoryCache();
            services.AddResponseCachingServices();
            services.TryAdd(ServiceDescriptor.Singleton<IResponseCache, DistributedResponseCache>());

            return services;
        }

        private static IServiceCollection AddResponseCachingServices(this IServiceCollection services)
        {
            services.TryAdd(ServiceDescriptor.Singleton<ICacheKeyProvider, CacheKeyProvider>());
            services.TryAdd(ServiceDescriptor.Singleton<ICacheabilityValidator, CacheabilityValidator>());

            return services;
        }
    }
}
