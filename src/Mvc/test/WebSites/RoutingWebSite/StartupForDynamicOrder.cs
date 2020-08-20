// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RoutingWebSite
{
    // For by tests for dynamic routing to pages/controllers
    public class StartupForDynamicOrder
    {
        public static class DynamicOrderScenarios
        {
            public const string AttributeRouteDynamicRoute = nameof(AttributeRouteDynamicRoute);
            public const string MultipleDynamicRoute = nameof(MultipleDynamicRoute);
            public const string ConventionalRouteDynamicRoute = nameof(ConventionalRouteDynamicRoute);
        }

        public IConfiguration Configuration { get; }

        public StartupForDynamicOrder(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc()
                .AddNewtonsoftJson()
                .SetCompatibilityVersion(CompatibilityVersion.Latest);

            services.AddTransient<Transformer>();
            services.AddScoped<TestResponseGenerator>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

            // Used by some controllers defined in this project.
            services.Configure<RouteOptions>(options => options.ConstraintMap["slugify"] = typeof(SlugifyParameterTransformer));
        }

        public void Configure(IApplicationBuilder app)
        {
            var scenario = Configuration.GetValue<string>("Scenario");
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                // Route order definition is important for all these routes:
                switch (scenario)
                {
                    case DynamicOrderScenarios.AttributeRouteDynamicRoute:
                        endpoints.MapDynamicControllerRoute<Transformer>("attribute-dynamic-order/{**slug}", new DynamicVersion() { Version = "slug" });
                        endpoints.MapControllers();
                        break;
                    case DynamicOrderScenarios.ConventionalRouteDynamicRoute:
                        endpoints.MapControllerRoute(null, "conventional-dynamic-order-before", new { controller = "DynamicOrder", action = "Index" });
                        endpoints.MapDynamicControllerRoute<Transformer>("{conventional-dynamic-order}", new DynamicVersion() { Version = "slug" });
                        endpoints.MapControllerRoute(null, "conventional-dynamic-order-after", new { controller = "DynamicOrder", action = "Index" });
                        break;
                    case DynamicOrderScenarios.MultipleDynamicRoute:
                        endpoints.MapDynamicControllerRoute<Transformer>("dynamic-order/{**slug}", new DynamicVersion() { Version = "slug" });
                        endpoints.MapDynamicControllerRoute<Transformer>("dynamic-order/specific/{**slug}", new DynamicVersion() { Version = "specific" });
                        break;
                    default:
                        throw new InvalidOperationException("Invalid scenario configuration.");
                }
            });

            app.Map("/afterrouting", b => b.Run(c =>
            {
                return c.Response.WriteAsync("Hello from middleware after routing");
            }));
        }

        private class Transformer : DynamicRouteValueTransformer
        {
            // Turns a format like `controller=Home,action=Index` into an RVD
            public override ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
            {
                var kvps = ((string)values?["slug"])?.Split("/")?.LastOrDefault()?.Split(",") ?? Array.Empty<string>();

                // Go to index by default if the route doesn't follow the slug pattern, we want to make sure always match to
                // test the order is applied
                var results = new RouteValueDictionary();
                results["controller"] = "Home";
                results["action"] = "Index";

                foreach (var kvp in kvps)
                {
                    var split = kvp.Split("=");
                    if (split.Length == 2)
                    {
                        results[split[0]] = split[1];
                    }
                }

                results["version"] = ((DynamicVersion)State).Version;

                return new ValueTask<RouteValueDictionary>(results);
            }
        }
    }
}
