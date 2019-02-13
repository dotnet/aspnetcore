// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for <see cref="IApplicationBuilder"/> to add MVC to the request execution pipeline.
    /// </summary>
    public static class MvcApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds MVC to the <see cref="IApplicationBuilder"/> request execution pipeline.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        /// <remarks>This method only supports attribute routing. To add conventional routes use
        /// <see cref="UseMvc(IApplicationBuilder, Action{IRouteBuilder})"/>.</remarks>
        public static IApplicationBuilder UseMvc(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMvc(routes =>
            {
            });
        }

        /// <summary>
        /// Adds MVC to the <see cref="IApplicationBuilder"/> request execution pipeline
        /// with a default route named 'default' and the following template:
        /// '{controller=Home}/{action=Index}/{id?}'.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseMvcWithDefaultRoute(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        /// <summary>
        /// Adds MVC to the <see cref="IApplicationBuilder"/> request execution pipeline.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="configureRoutes">A callback to configure MVC routes.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseMvc(
            this IApplicationBuilder app,
            Action<IRouteBuilder> configureRoutes)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (configureRoutes == null)
            {
                throw new ArgumentNullException(nameof(configureRoutes));
            }

            // Verify if AddMvc was done before calling UseMvc
            // We use the MvcMarkerService to make sure if all the services were added.
            if (app.ApplicationServices.GetService(typeof(MvcMarkerService)) == null)
            {
                throw new InvalidOperationException(Resources.FormatUnableToFindServices(
                    nameof(IServiceCollection),
                    "AddMvc",
                    "ConfigureServices(...)"));
            }

            var middlewarePipelineBuilder = app.ApplicationServices.GetRequiredService<MiddlewareFilterBuilder>();
            middlewarePipelineBuilder.ApplicationBuilder = app.New();

            var routes = new RouteBuilder(app)
            {
                DefaultHandler = app.ApplicationServices.GetRequiredService<MvcRouteHandler>(),
            };

            configureRoutes(routes);

            routes.Routes.Insert(0, AttributeRouting.CreateAttributeMegaRoute(app.ApplicationServices));

            return app.UseRouter(routes.Build());
        }
    }
}
