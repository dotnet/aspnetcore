// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace RoutingWebSite
{
    public class StartupWithGlobalRouting
    {
        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();

            services.AddMvc();

            services.AddScoped<TestResponseGenerator>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseGlobalRouting();

            app.UseMvcWithEndpoint(routes =>
            {
                routes.MapAreaEndpoint(
                   "flightRoute",
                   "adminRoute",
                   "{area:exists}/{controller}/{action}",
                   new { controller = "Home", action = "Index" },
                   new { area = "Travel" });

                routes.MapEndpoint(
                    "ActionAsMethod",
                    "{controller}/{action}",
                    defaults: new { controller = "Home", action = "Index" });

                routes.MapEndpoint(
                    "RouteWithOptionalSegment",
                    "{controller}/{action}/{path?}");
            });
        }
    }
}