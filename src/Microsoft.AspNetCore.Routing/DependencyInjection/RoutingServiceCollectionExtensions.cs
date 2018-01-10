// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.AspNetCore.Routing.Tree;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains extension methods to <see cref="IServiceCollection"/>.
    /// </summary>
    public static class RoutingServiceCollectionExtensions
    {
        /// <summary>
        /// Adds services required for routing requests.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddRouting(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAddTransient<IInlineConstraintResolver, DefaultInlineConstraintResolver>();
            services.TryAddSingleton(UrlEncoder.Default);
            services.TryAddSingleton<ObjectPool<UriBuildingContext>>(s =>
            {
                var provider = s.GetRequiredService<ObjectPoolProvider>();
                var encoder = s.GetRequiredService<UrlEncoder>();
                return provider.Create<UriBuildingContext>(new UriBuilderContextPooledObjectPolicy(encoder));
            });

            // The TreeRouteBuilder is a builder for creating routes, it should stay transient because it's
            // stateful.
            services.TryAddTransient<TreeRouteBuilder>();

            services.TryAddSingleton(typeof(RoutingMarkerService));

            return services;
        }

        /// <summary>
        /// Adds services required for routing requests.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="configureOptions">The routing options to configure the middleware with.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddRouting(
            this IServiceCollection services,
            Action<RouteOptions> configureOptions)
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
            services.AddRouting();

            return services;
        }
    }
}