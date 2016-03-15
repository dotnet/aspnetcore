// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.TagHelpers.Cache;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
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

            builder.Services.TryAddTransient<IDistributedCacheTagHelperStorage, DistributedCacheTagHelperStorage>();
            builder.Services.TryAddTransient<IDistributedCacheTagHelperFormatter, DistributedCacheTagHelperFormatter>();
            builder.Services.TryAddSingleton<IDistributedCacheTagHelperService, DistributedCacheTagHelperService>();

            // Required default services for cache tag helpers
            builder.Services.TryAddSingleton<IDistributedCache, MemoryDistributedCache>();
            builder.Services.TryAddSingleton<IMemoryCache, MemoryCache>();

            return builder;
        }
    }
}