// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for adding the <see cref="RouterMiddleware"/> middleware to an <see cref="IApplicationBuilder"/>.
    /// </summary>
    public static class RoutingBuilderExtensions
    {
        /// <summary>
        /// Adds a <see cref="RouterMiddleware"/> middleware to the specified <see cref="IApplicationBuilder"/> with the specified <see cref="IRouter"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <param name="router">The <see cref="IRouter"/> to use for routing requests.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseRouter(this IApplicationBuilder builder, IRouter router)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (router == null)
            {
                throw new ArgumentNullException(nameof(router));
            }

            if (builder.ApplicationServices.GetService(typeof(RoutingMarkerService)) == null)
            {
                throw new InvalidOperationException(Resources.FormatUnableToFindServices(
                    nameof(IServiceCollection),
                    nameof(RoutingServiceCollectionExtensions.AddRouting),
                    "ConfigureServices(...)"));
            }

            return builder.UseMiddleware<RouterMiddleware>(router);
        }

        /// <summary>
        /// Adds a <see cref="RouterMiddleware"/> middleware to the specified <see cref="IApplicationBuilder"/>
        /// with the <see cref="IRouter"/> built from configured <see cref="IRouteBuilder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <param name="action">An <see cref="Action{IRouteBuilder}"/> to configure the provided <see cref="IRouteBuilder"/>.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseRouter(this IApplicationBuilder builder, Action<IRouteBuilder> action)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (builder.ApplicationServices.GetService(typeof(RoutingMarkerService)) == null)
            {
                throw new InvalidOperationException(Resources.FormatUnableToFindServices(
                    nameof(IServiceCollection),
                    nameof(RoutingServiceCollectionExtensions.AddRouting),
                    "ConfigureServices(...)"));
            }

            var routeBuilder = new RouteBuilder(builder);
            action(routeBuilder);

            return builder.UseRouter(routeBuilder.Build());
        }
    }
}
