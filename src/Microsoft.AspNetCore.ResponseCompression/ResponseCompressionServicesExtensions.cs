// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for the ResponseCompression middleware.
    /// </summary>
    public static class ResponseCompressionServicesExtensions
    {
        /// <summary>
        /// Add response compression services.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
        /// <returns></returns>
        public static IServiceCollection AddResponseCompression(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAddSingleton<IResponseCompressionProvider, ResponseCompressionProvider>();
            return services;
        }

        /// <summary>
        /// Add response compression services and configure the related options.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
        /// <param name="configureOptions">A delegate to configure the <see cref="ResponseCompressionOptions"/>.</param>
        /// <returns></returns>
        public static IServiceCollection AddResponseCompression(this IServiceCollection services, Action<ResponseCompressionOptions> configureOptions)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            services.Configure(configureOptions);
            services.TryAddSingleton<IResponseCompressionProvider, ResponseCompressionProvider>();
            return services;
        }
    }
}
