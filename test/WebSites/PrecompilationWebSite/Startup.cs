// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace PrecompilationWebSite
{
    public class Startup
    {
        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            // Add MVC services to the services container
            services
                .AddMvc()
                .AddPrecompiledRazorViews(GetType().GetTypeInfo().Assembly);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCultureReplacer();

            app.UseMvcWithDefaultRoute();
        }

        public static void Main(string[] args)
        {
            var application = new WebApplicationBuilder()
                .UseConfiguration(WebApplicationConfiguration.GetDefault(args))
                .UseStartup<Startup>()
                .Build();

            application.Run();
        }
    }
}
