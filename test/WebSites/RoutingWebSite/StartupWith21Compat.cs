// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RoutingWebSite
{
    public class StartupWith21Compat
    {
        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddScoped<TestResponseGenerator>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

            // EndpointRoutingController is not compatible with old routing
            // Remove its action to avoid errors
            var actionDescriptorProvider = new RemoveControllerActionDescriptorProvider(typeof(EndpointRoutingController));
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IActionDescriptorProvider>(actionDescriptorProvider));
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    "DataTokensRoute",
                    "DataTokensRoute/{controller}/{action}",
                    defaults: null,
                    constraints: new { controller = "DataTokens" },
                    dataTokens: new { hasDataTokens = true });

                routes.MapAreaRoute(
                    "flightRoute",
                    "adminRoute",
                    "{area:exists}/{controller}/{action}",
                    defaults: new { controller = "Home", action = "Index" },
                    constraints: new { area = "Travel" });

                routes.MapRoute(
                    "ActionAsMethod",
                    "{controller}/{action}",
                    defaults: new { controller = "Home", action = "Index" });

                routes.MapRoute(
                    "RouteWithOptionalSegment",
                    "{controller}/{action}/{path?}");
            });

            app.Map("/afterrouting", b => b.Run(c =>
            {
                return c.Response.WriteAsync("Hello from middleware after routing");
            }));
        }
    }
}