// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace RoutingSample.Web
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
        }

        public void Configure(IApplicationBuilder builder)
        {
            var endpoint1 = new RouteHandler((c) =>
            {
                return c.Response.WriteAsync($"match1, route values - {string.Join(", ", c.GetRouteData().Values)}");
            });

            var endpoint2 = new RouteHandler((c) => c.Response.WriteAsync("Hello, World!"));

            var routeBuilder = new RouteBuilder()
            {
                DefaultHandler = endpoint1,
                ServiceProvider = builder.ApplicationServices,
            };

            routeBuilder.AddPrefixRoute("api/store", endpoint1);
            routeBuilder.AddPrefixRoute("hello/world", endpoint2);

            routeBuilder.MapLocaleRoute("en-US", "store/US/{action}", new { controller = "Store" });
            routeBuilder.MapLocaleRoute("en-GB", "store/UK/{action}", new { controller = "Store" });

            routeBuilder.AddPrefixRoute("", endpoint2);

            builder.UseRouter(routeBuilder.Build());
        }
    }
}