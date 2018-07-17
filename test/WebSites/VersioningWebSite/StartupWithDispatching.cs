// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace VersioningWebSite
{
    public class StartupWithDispatching
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDispatcher();

            // Add MVC services to the services container
            services.AddMvc();

            services.AddScoped<TestResponseGenerator>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDispatcher();

            app.UseMvcWithEndpoint(endpoints =>
            {
                endpoints.MapEndpoint(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}