// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods for <see cref="IApplicationBuilder"/> to add Mvc to the request execution pipeline.
    /// </summary>
    public static class BuilderExtensions
    {
        /// <summary>
        /// Adds Mvc to the <see cref="IApplicationBuilder"/> request execution pipeline.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <returns>The <paramref name="app"/>.</returns>
        /// <remarks>This method only supports attribute routing. To add conventional routes use
        /// <see cref="UseMvc(IApplicationBuilder, Action{IRouteBuilder})"/>.</remarks>
        public static IApplicationBuilder UseMvc([NotNull] this IApplicationBuilder app)
        {
            return app.UseMvc(routes =>
            {
            });
        }

        /// <summary>
        /// Adds Mvc to the <see cref="IApplicationBuilder"/> request execution pipeline.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="configureRoutes">A callback to configure Mvc routes.</param>
        /// <returns>The <paramref name="app"/>.</returns>
        public static IApplicationBuilder UseMvc(
            [NotNull] this IApplicationBuilder app,
            [NotNull] Action<IRouteBuilder> configureRoutes)
        {
            // Verify if AddMvc was done before calling UseMvc
            // We use the MvcMarkerService to make sure if all the services were added.
            MvcServicesHelper.ThrowIfMvcNotRegistered(app.ApplicationServices);

            var routes = new RouteBuilder
            {
                DefaultHandler = new MvcRouteHandler(),
                ServiceProvider = app.ApplicationServices
            };

            configureRoutes(routes);

            // Adding the attribute route comes after running the user-code because
            // we want to respect any changes to the DefaultHandler.
            routes.Routes.Insert(0, AttributeRouting.CreateAttributeMegaRoute(
                routes.DefaultHandler,
                app.ApplicationServices));

            return app.UseRouter(routes.Build());
        }
    }
}