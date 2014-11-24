// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Constraints;

namespace RoutingSample.Web
{
    public class Startup
    {
        public void Configure(IApplicationBuilder builder)
        {
            builder.UseServices(services =>
            {
                services.AddRouting();
            });

            var endpoint1 = new DelegateRouteEndpoint(async (context) =>
                                                        await context
                                                                .HttpContext
                                                                .Response
                                                                .WriteAsync(
                                                                  "match1, route values -" + context.RouteData.Values.Print()));

            var endpoint2 = new DelegateRouteEndpoint(async (context) =>
                                                        await context
                                                                .HttpContext
                                                                .Response
                                                                .WriteAsync("Hello, World!"));

            var routeBuilder = new RouteBuilder();
            routeBuilder.DefaultHandler = endpoint1;
            routeBuilder.ServiceProvider = builder.ApplicationServices;

            routeBuilder.AddPrefixRoute("api/store");

            routeBuilder.MapRoute("defaultRoute",
                                  "api/constraint/{controller}",
                                  null,
                                  new { controller = "my.*" });
            routeBuilder.MapRoute("regexStringRoute",
                                  "api/rconstraint/{controller}",
                                  new { foo = "Bar" },
                                  new { controller = new RegexRouteConstraint("^(my.*)$") });
            routeBuilder.MapRoute("regexRoute",
                                  "api/r2constraint/{controller}",
                                  new { foo = "Bar2" },
                                  new { controller = new RegexRouteConstraint(new Regex("^(my.*)$")) });

            routeBuilder.MapRoute("parameterConstraintRoute",
                                  "api/{controller}/{*extra}",
                                  new { controller = "Store" });

            routeBuilder.AddPrefixRoute("hello/world", endpoint2);

            routeBuilder.MapLocaleRoute("en-US", "store/US/{action}", new { controller = "Store" });
            routeBuilder.MapLocaleRoute("en-GB", "store/UK/{action}", new { controller = "Store" });

            routeBuilder.AddPrefixRoute("", endpoint2);

            builder.UseRouter(routeBuilder.Build());
        }
    }
}