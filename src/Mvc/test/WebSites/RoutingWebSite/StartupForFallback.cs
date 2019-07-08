// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace RoutingWebSite
{
    // For by tests for fallback routing to pages/controllers
    public class StartupForFallback
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc()
                .AddNewtonsoftJson()
                .SetCompatibilityVersion(CompatibilityVersion.Latest);

            // Used by some controllers defined in this project.
            services.Configure<RouteOptions>(options => options.ConstraintMap["slugify"] = typeof(SlugifyParameterTransformer));
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapFallbackToAreaController("admin/{*path:nonfile}", "Index", "Fallback", "Admin");
                endpoints.MapFallbackToPage("/FallbackPage");

                endpoints.MapControllerRoute("admin", "link_generation/{area}/{controller}/{action}/{id?}");
            });

            app.Map("/afterrouting", b => b.Run(c =>
            {
                return c.Response.WriteAsync("Hello from middleware after routing");
            }));
        }
    }
}
