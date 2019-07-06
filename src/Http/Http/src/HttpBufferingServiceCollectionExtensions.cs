// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up Http Buffering services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class HttpBufferingServiceCollectionExtensions
    {
        /// <summary>
        /// Adds services required to enable Http Bufering to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddHttpBuffering(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions();

            services.TryAddSingleton<IFileBufferingStreamFactory, HttpFileBufferingStreamFactory>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<HttpBufferingOptions>, PostConfigureHttpBufferingOptions>());

            return services;
        }

        /// <summary>
        /// Adds services required to enable Http Bufering to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="configureOptions">An <see cref="Action{HttpBufferingOptions}"/> to configure the provided <see cref="HttpBufferingOptions"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddHttpBuffering(this IServiceCollection services, Action<HttpBufferingOptions> configureOptions)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            services.AddHttpBuffering();
            services.Configure(configureOptions);

            return services;
        }
    }
}
