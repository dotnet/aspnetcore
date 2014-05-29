using System;
using System.Text.RegularExpressions;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Constraints;
using Microsoft.Framework.DependencyInjection;

namespace RoutingSample.Web
{
    public class Startup
    {
        public void Configure(IBuilder builder)
        {
            var collectionBuilder = new RouteBuilder();

            var endpoint1 = new DelegateRouteEndpoint(async (context) =>
                                                        await context
                                                                .HttpContext
                                                                .Response
                                                                .WriteAsync(
                                                                  "match1, route values -" + context.Values.Print()));

            var endpoint2 = new DelegateRouteEndpoint(async (context) => 
                                                        await context
                                                                .HttpContext
                                                                .Response
                                                                .WriteAsync("Hello, World!"));

            collectionBuilder.DefaultHandler = endpoint1;
            collectionBuilder.ServiceProvider = builder.ApplicationServices;

            collectionBuilder.AddPrefixRoute("api/store");

            collectionBuilder.MapRoute("defaultRoute",
                                       "api/constraint/{controller}",
                                       null,
                                       new { controller = "my.*" });
            collectionBuilder.MapRoute("regexStringRoute", 
                                       "api/rconstraint/{controller}",
                                       new { foo = "Bar" },
                                       new { controller = new RegexConstraint("^(my.*)$") });
            collectionBuilder.MapRoute("regexRoute",
                                       "api/r2constraint/{controller}",
                                       new { foo = "Bar2" },
                                       new { controller = new RegexConstraint(new Regex("^(my.*)$")) });

            collectionBuilder.MapRoute("parameterConstraintRoute",
                                       "api/{controller}/{*extra}",
                                       new { controller = "Store" });

            collectionBuilder.AddPrefixRoute("hello/world", endpoint2);
            collectionBuilder.AddPrefixRoute("", endpoint2);
            builder.UseRouter(collectionBuilder.Build());
        }
    }
}