﻿using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Routing;

namespace RoutingSample.Web
{
    public class Startup
    {
        public void Configuration(IBuilder builder)
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