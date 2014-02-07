﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

#if NET45

using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Routing.Owin;
using Microsoft.AspNet.Routing.Template;
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

            routes.Add(new PrefixRoute(endpoint1, "api/store"));
            routes.Add(new TemplateRoute(endpoint1, "api/checkout/{*extra}"));
            routes.Add(new PrefixRoute(endpoint2, "hello/world"));
            routes.Add(new PrefixRoute(endpoint1, ""));
        }
    }
}

#endif
