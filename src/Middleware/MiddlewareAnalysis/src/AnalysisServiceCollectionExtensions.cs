// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.MiddlewareAnalysis;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up diagnostic services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class AnalysisServiceCollectionExtensions
    {
        /// <summary>
        /// Adds diagnostic services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddMiddlewareAnalysis(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Prevent registering the same implementation of IStartupFilter (AnalysisStartupFilter) multiple times.
            // But allow multiple registrations of different implementation types.
            services.TryAddEnumerable(ServiceDescriptor.Transient<IStartupFilter, AnalysisStartupFilter>());
            return services;
        }
    }
}
