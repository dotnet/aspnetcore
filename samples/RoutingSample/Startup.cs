﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

#if NET45

using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Routing;
using Owin;

namespace RoutingSample
{
    internal class Startup
    {
        public void Configuration(IAppBuilder builder)
        {
            builder.UseErrorPage();

            builder.UseBuilder(ConfigureRoutes);
        }

        private void ConfigureRoutes(IBuilder builder)
        {            
            var routes = builder.UseRouter();

            var endpoint1 = new HttpContextRouteEndpoint(async (context) => await context.Response.WriteAsync("match1"));
            var endpoint2 = new HttpContextRouteEndpoint(async (context) => await context.Response.WriteAsync("Hello, World!"));

            routes.DefaultHandler = endpoint1;
            routes.AddPrefixRoute("api/store");
            routes.MapRoute("api/{controller}/{*extra}", new { controller = "Store" });

            routes.AddPrefixRoute("hello/world", endpoint2);
            routes.AddPrefixRoute("", endpoint2);
        }
    }
}

#endif
