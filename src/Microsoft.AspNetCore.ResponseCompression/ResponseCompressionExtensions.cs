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
    public static class ResponseCompressionExtensions
    {
        /// <summary>
        /// Add response compression services and enable compression for responses with the given MIME types.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
        /// <param name="mimeTypes">Response Content-Type MIME types to enable compression for.</param>
        /// <returns></returns>
        public static IServiceCollection AddResponseCompression(this IServiceCollection services, params string[] mimeTypes)
        {
            return services.AddResponseCompression(options =>
            {
                options.MimeTypes = mimeTypes;
            });
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
            services.TryAddTransient<IResponseCompressionProvider, ResponseCompressionProvider>();
            return services;
        }

        /// <summary>
        /// Adds middleware for dynamically compressing HTTP Responses.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
        public static IApplicationBuilder UseResponseCompression(this IApplicationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.UseMiddleware<ResponseCompressionMiddleware>();
        }
    }
}
