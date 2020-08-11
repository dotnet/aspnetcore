// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Service extension methods for the <see cref="DatabaseDeveloperPageExceptionFilter"/>.
    /// </summary>
    public static class DatabaseDeveloperPageExceptionFilterServiceExtensions
    {
        /// <summary>
        /// Add response caching services.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
        /// <returns></returns>
        public static IServiceCollection AddDatabaseDeveloperPageExceptionFilter(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAddEnumerable(new ServiceDescriptor(typeof(IDeveloperPageExceptionFilter), typeof(DatabaseDeveloperPageExceptionFilter), ServiceLifetime.Singleton));

            return services;
        }
    }
}
