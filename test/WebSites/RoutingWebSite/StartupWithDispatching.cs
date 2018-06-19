// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matchers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace RoutingWebSite
{
    public class StartupWithDispatching
    {
        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDispatcher();

            services.AddMvc();

            services.AddScoped<TestResponseGenerator>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDispatcher();

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