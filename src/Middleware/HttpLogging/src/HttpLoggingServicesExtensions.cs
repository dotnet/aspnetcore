// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.HttpLogging;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for the HttpLogging middleware.
    /// </summary>
    public static class HttpLoggingServicesExtensions
    {
        /// <summary>
        /// Adds HTTP Logging services.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
        /// <param name="configureOptions">A delegate to configure the <see cref="HttpLoggingOptions"/>.</param>
        /// <returns></returns>
        public static IServiceCollection ConfigureHttpLogging(this IServiceCollection services, Action<HttpLoggingOptions> configureOptions)
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
            return services;
        }
    }
}
