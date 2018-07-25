// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace BasicWebSite
{
    public class StartupWithGlobalRouting
    {
        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();

            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Latest) // this compat version enables global routing
                .AddXmlDataContractSerializerFormatters();

            services.ConfigureBaseWebSiteAuthPolicies();

            services.AddHttpContextAccessor();
            services.AddScoped<RequestIdService>();
        }

        public void Configure(IApplicationBuilder app)
        {
            // Initializes the RequestId service for each request
            app.UseMiddleware<RequestIdMiddleware>();

            app.UseGlobalRouting();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    "ActionAsMethod",
                    "{controller}/{action}",
                    defaults: new { controller = "Home", action = "Index" });
            });
        }
    }
}