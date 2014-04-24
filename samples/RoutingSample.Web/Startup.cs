using System.Text.RegularExpressions;
using Microsoft.AspNet;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Routing;

namespace RoutingSample.Web
{
    public class Startup
    {
        public void Configuration(IBuilder builder)
        {
            var routes = new RouteCollection();
            builder.UseRouter(routes);

            var endpoint1 = new DelegateRouteEndpoint(async (context) =>
                                                            await context.HttpContext.Response.WriteAsync(
                                                            "match1, route values -" + context.Values.Print()));
            var endpoint2 = new DelegateRouteEndpoint(async (context) => 
                                                        await context.HttpContext.Response.WriteAsync("Hello, World!"));

            routes.DefaultHandler = endpoint1;
            routes.AddPrefixRoute("api/store");

            routes.MapRoute("defaultRoute", "api/constraint/{controller}", null, new { controller = "my.*" });
            routes.MapRoute("regexStringRoute", 
                            "api/rconstraint/{controller}",
                            new { foo = "Bar" },
                            new { controller = new RegexConstraint("^(my.*)$") });
            routes.MapRoute("regexRoute",
                            "api/r2constraint/{controller}",
                            new { foo = "Bar2" },
                            new { controller = new RegexConstraint(new Regex("^(my.*)$")) });

            routes.MapRoute("parameterConstraintRoute", "api/{controller}/{*extra}", new { controller = "Store" });

            routes.AddPrefixRoute("hello/world", endpoint2);
            routes.AddPrefixRoute("", endpoint2);
        }
    }
}