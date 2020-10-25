// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for the exception handler middleware.
    /// </summary>
    public static class ExceptionHandlerServiceCollectionExtensions
    {
        /// <summary>
        /// Adds services and options for the exception handler middleware.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
        /// <param name="configureOptions">A delegate to configure the <see cref="ExceptionHandlerOptions"/>.</param>
        /// <returns></returns>
        public static IServiceCollection AddExceptionHandler(this IServiceCollection services, Action<ExceptionHandlerOptions> configureOptions)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }
            
            return services.Configure(configureOptions);
        }

        /// <summary>
        /// Adds services and options for the exception handler middleware.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
        /// <param name="configureOptions">A delegate to configure the <see cref="ExceptionHandlerOptions"/>.</param>
        /// <returns></returns>
        public static IServiceCollection AddExceptionHandler<TService>(this IServiceCollection services, Action<ExceptionHandlerOptions, TService> configureOptions) where TService : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }
            
            services.AddOptions<ExceptionHandlerOptions>().Configure(configureOptions);
            return services;
        }
    }
}
