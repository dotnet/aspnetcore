// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace BasicWebSite
{
    public class StartupWithEndpointRouting
    {
        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();

            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Latest)
                .AddXmlDataContractSerializerFormatters();

            services.ConfigureBaseWebSiteAuthPolicies();

            services.AddHttpContextAccessor();
            services.AddScoped<RequestIdService>();
            services.AddScoped<TestResponseGenerator>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();

            // Initializes the RequestId service for each request
            app.UseMiddleware<RequestIdMiddleware>();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    "ActionAsMethod",
                    "{controller}/{action}",
                    defaults: new { controller = "Home", action = "Index" });

                routes.MapRoute("PageRoute", "{controller}/{action}/{page}");
            });
        }
    }
}
