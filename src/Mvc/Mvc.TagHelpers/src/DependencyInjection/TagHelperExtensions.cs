// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring Razor cache tag helpers.
    /// </summary>
    public static class TagHelperServicesExtensions
    {
        /// <summary>
        ///  Adds MVC cache tag helper services to the application.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
        /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
        public static IMvcCoreBuilder AddCacheTagHelper(this IMvcCoreBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            AddCacheTagHelperServices(builder.Services);

            return builder;
        }

        /// <summary>
        ///  Configures the memory size limits on the cache of the <see cref="CacheTagHelper"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <param name="configure">The <see cref="Action{CacheTagHelperOptions}"/>to configure the cache options.</param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder AddCacheTagHelperLimits(this IMvcBuilder builder, Action<CacheTagHelperOptions> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.Services.Configure(configure);

            return builder;
        }

        /// <summary>
        ///  Configures the memory size limits on the cache of the <see cref="CacheTagHelper"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
        /// <param name="configure">The <see cref="Action{CacheTagHelperOptions}"/>to configure the cache options.</param>
        /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
        public static IMvcCoreBuilder AddCacheTagHelperLimits(this IMvcCoreBuilder builder, Action<CacheTagHelperOptions> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.Services.Configure(configure);

            return builder;
        }

        internal static void AddCacheTagHelperServices(IServiceCollection services)
        {
            services.TryAddSingleton<IDistributedCacheTagHelperStorage, DistributedCacheTagHelperStorage>();
            services.TryAddSingleton<IDistributedCacheTagHelperFormatter, DistributedCacheTagHelperFormatter>();
            services.TryAddSingleton<IDistributedCacheTagHelperService, DistributedCacheTagHelperService>();

            // Required default services for cache tag helpers
            services.AddDistributedMemoryCache();
            services.TryAddSingleton<CacheTagHelperMemoryCacheFactory>();
        }
    }
}