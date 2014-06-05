using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Constraints;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;

namespace RoutingSample.Web
{
    public class Startup
    {
        public void Configure(IBuilder builder)
        {
            builder.UseServices(services =>
            {
                services.Add(RoutingServices.GetDefaultServices());
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
                                  new { controller = new RegexConstraint("^(my.*)$") });
            routeBuilder.MapRoute("regexRoute",
                                  "api/r2constraint/{controller}",
                                  new { foo = "Bar2" },
                                  new { controller = new RegexConstraint(new Regex("^(my.*)$")) });

            routeBuilder.MapRoute("parameterConstraintRoute",
                                  "api/{controller}/{*extra}",
                                  new { controller = "Store" });

            routeBuilder.AddPrefixRoute("hello/world", endpoint2);
            routeBuilder.AddPrefixRoute("", endpoint2);
            builder.UseRouter(routeBuilder.Build());
        }
    }
}