// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet.Builder
{
    public static class BuilderExtensions
    {
        public static IBuilder UseMvc([NotNull] this IBuilder app)
        {
            return app.UseMvc(routes =>
            {
                // Action style actions
                routes.MapRoute(null, "{controller}/{action}/{id?}", new { controller = "Home", action = "Index" });

                // Rest style actions
                routes.MapRoute(null, "{controller}/{id?}");
            });
        }

        public static IBuilder UseMvc([NotNull] this IBuilder app, [NotNull] Action<IRouteBuilder> configureRoutes)
        {
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