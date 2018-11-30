// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for the HttpsRedirection middleware.
    /// </summary>
    public static class HttpsRedirectionServicesExtensions
    {
        /// <summary>
        /// Adds HTTPS redirection services.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
        /// <param name="configureOptions">A delegate to configure the <see cref="HttpsRedirectionOptions"/>.</param>
        /// <returns></returns>
        public static IServiceCollection AddHttpsRedirection(this IServiceCollection services, Action<HttpsRedirectionOptions> configureOptions)
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
