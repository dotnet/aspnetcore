// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace WebApiCompatShimWebSite
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Add MVC services to the services container
            services.AddMvc()
                .AddWebApiConventions()
                .SetCompatibilityVersion(CompatibilityVersion.Latest)
                .AddMvcOptions(options => options.EnableEndpointRouting = false);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMvc(routes =>
            {
                // Tests include different styles of WebAPI conventional routing and action selection - the prefix keeps
                // them from matching too eagerly.
                routes.MapWebApiRoute("named-action", "api/Blog/{controller}/{action}/{id?}");
                routes.MapWebApiRoute("unnamed-action", "api/Admin/{controller}/{id?}");
                routes.MapWebApiRoute("name-as-parameter", "api/Store/{controller}/{name?}");
                routes.MapWebApiRoute("extra-parameter", "api/Support/{extra}/{controller}/{id?}");

                // This route can't access any of our webapi controllers
                routes.MapRoute("default", "{controller}/{action}/{id?}");
            });
        }

        public static void Main(string[] args)
        {
            var host = CreateWebHostBuilder(args)
                .Build();

            host.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseKestrel()
                .UseIISIntegration();
    }
}

