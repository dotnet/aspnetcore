// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace RoutingWebSite
{
    // For by tests for dynamic routing to pages/controllers
    public class StartupForDynamic
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc()
                .AddNewtonsoftJson()
                .SetCompatibilityVersion(CompatibilityVersion.Latest);

            services.AddSingleton<Transformer>();

            // Used by some controllers defined in this project.
            services.Configure<RouteOptions>(options => options.ConstraintMap["slugify"] = typeof(SlugifyParameterTransformer));
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDynamicControllerRoute<Transformer>("dynamic/{**slug}");
                endpoints.MapDynamicPageRoute<Transformer>("dynamicpage/{**slug}");

                endpoints.MapControllerRoute("link", "link_generation/{controller}/{action}/{id?}");

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
                var kvps = ((string)values["slug"]).Split(",");

                var results = new RouteValueDictionary();
                foreach (var kvp in kvps)
                {
                    var split = kvp.Split("=");
                    results[split[0]] = split[1];
                }

                return new ValueTask<RouteValueDictionary>(results);
            }
        }
    }
}
