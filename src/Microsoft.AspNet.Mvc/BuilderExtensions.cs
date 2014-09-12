// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet.Builder
{
    public static class BuilderExtensions
    {
        public static IApplicationBuilder UseMvc([NotNull] this IApplicationBuilder app)
        {
            return app.UseMvc(routes =>
            {
                // Action style actions
                routes.MapRoute(null, "{controller}/{action}/{id?}", new { controller = "Home", action = "Index" });

                // Rest style actions
                routes.MapRoute(null, "{controller}/{id?}");
            });
        }

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

            routes.Routes.Add(AttributeRouting.CreateAttributeMegaRoute(
                routes.DefaultHandler, 
                app.ApplicationServices));

            configureRoutes(routes);

            return app.UseRouter(routes.Build());
        }
    }
}