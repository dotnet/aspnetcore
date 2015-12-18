// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using System.Threading;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
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

        public void Configure(IApplicationBuilder app)
        {
            var endpoint1 = new RouteHandler((c) =>
            {
                return c.Response.WriteAsync($"match1, route values - {string.Join(", ", c.GetRouteData().Values)}");
            });

            var endpoint2 = new RouteHandler((c) => c.Response.WriteAsync("Hello, World!"));

            var routeBuilder = new RouteBuilder(app)
            {
                DefaultHandler = endpoint1,
            };

            routeBuilder.MapRoute("api/status/{item}", c => c.Response.WriteAsync($"{c.GetRouteValue("item")} is just fine."));
            routeBuilder.MapRoute("localized/{lang=en-US}", b =>
            {
                b.Use(next => async (c) =>
                {
                    var culture = new CultureInfo((string)c.GetRouteValue("lang"));
#if DNX451
                    Thread.CurrentThread.CurrentCulture = culture;
                    Thread.CurrentThread.CurrentUICulture = culture;
#else
                    CultureInfo.CurrentCulture = culture;
                    CultureInfo.CurrentUICulture = culture;
#endif
                    await next(c);
                });

                b.Run(c => c.Response.WriteAsync($"What would you do with {1000000m:C}?"));
            });

            routeBuilder.AddPrefixRoute("api/store", endpoint1);
            routeBuilder.AddPrefixRoute("hello/world", endpoint2);

            routeBuilder.MapLocaleRoute("en-US", "store/US/{action}", new { controller = "Store" });
            routeBuilder.MapLocaleRoute("en-GB", "store/UK/{action}", new { controller = "Store" });

            routeBuilder.AddPrefixRoute("", endpoint2);

            app.UseRouter(routeBuilder.Build());
        }

        public static void Main(string[] args)
        {
            var application = new WebApplicationBuilder()
                .UseConfiguration(WebApplicationConfiguration.GetDefault(args))
                .UseStartup<Startup>()
                .Build();

            application.Run();
        }
    }
}