// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;

namespace EmbeddedViewSample.Web
{
    public class Startup
    {
        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            // Add MVC services to the services container
            services.AddMvc();

            services.Configure<RazorViewEngineOptions>(options =>
            {
                // Base namespace matches the resources added to the assembly from the EmbeddedResources folder.
                options.FileProvider = new EmbeddedFileProvider(
                    GetType().GetTypeInfo().Assembly,
                    baseNamespace: "EmbeddedViewSample.Web.EmbeddedResources");
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMvc(routes =>
            {
                routes.MapRoute("areaRoute", "{area:exists}/{controller}/{action}");
                routes.MapRoute(
                    "default",
                    "{controller}/{action}/{id?}",
                    new { controller = "Home", action = "Index" });
            });
        }
    }
}