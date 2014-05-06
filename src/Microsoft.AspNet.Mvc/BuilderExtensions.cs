// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet
{
    public static class BuilderExtensions
    {
        public static IBuilder UseMvc([NotNull] this IBuilder app)
        {
            return app.UseMvc(routes =>
            {
                routes.MapRoute(null, "{area}/{controller}/{action}", new { controller = "Home", action = "Index" });
                routes.MapRoute(null, "{area}/{controller}", new { controller = "Home" });
                routes.MapRoute(null, "{controller}/{action}", new { controller = "Home", action = "Index" });
                routes.MapRoute(null, "{controller}", new { controller = "Home" });
            });
        }

        public static IBuilder UseMvc([NotNull] this IBuilder app, [NotNull] Action<IRouteCollection> configureRoutes)
        {
            var routes = new RouteCollection
            {
                DefaultHandler = new MvcRouteHandler()
            };

            // REVIEW: Consider adding UseMvc() that automagically adds the default MVC route
            configureRoutes(routes);

            return app.UseRouter(routes);
        }
    }
}