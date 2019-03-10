// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.HeaderPropagation
{
    public static class HeaderPropagationExtensions
    {
        private static readonly string UnableToFindServices = string.Format(
            "Unable to find the required services. Please add all the required services by calling '{0}.{1}' inside the call to 'ConfigureServices(...)' in the application startup code.",
            nameof(IServiceCollection),
            nameof(AddHeaderPropagation));

        /// <summary>
        /// Adds services required for propagating headers to a <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddHeaderPropagation(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAddSingleton<HeaderPropagationState>();

            return services;
        }

        /// <summary>
        /// Adds services required for propagating headers to a <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="configureOptions">The <see cref="HeaderPropagationOptions"/> to configure the middleware with.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddHeaderPropagation(this IServiceCollection services, Action<HeaderPropagationOptions> configureOptions)
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
            services.AddHeaderPropagation();

            return services;
        }

        public static IHttpClientBuilder AddHeaderPropagation(this IHttpClientBuilder builder)
        {
            builder.Services.TryAddSingleton<HeaderPropagationState>();
            builder.Services.TryAddTransient<HeaderPropagationMessageHandler>();

            builder.AddHttpMessageHandler<HeaderPropagationMessageHandler>();

            return builder;
        }

        /// <summary>
        /// Adds a middleware that collect headers to be propagated to a <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <returns>A reference to the <paramref name="app"/> after the operation has completed.</returns>
        public static IApplicationBuilder UseHeaderPropagation(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (app.ApplicationServices.GetService<HeaderPropagationState>() == null)
            {
                throw new InvalidOperationException(UnableToFindServices);
            }

            return app.UseMiddleware<HeaderPropagationMiddleware>();
        }
    }
}
