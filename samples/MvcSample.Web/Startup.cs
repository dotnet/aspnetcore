// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Autofac;
using Autofac.Framework.DependencyInjection;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Dnx.Runtime;
using MvcSample.Web.Filters;
using MvcSample.Web.Services;

namespace MvcSample.Web
{
    public class Startup
    {
        private bool _autoFac;

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddCaching();
            services.AddSession();

            services.AddMvc(options =>
            {
                options.Filters.Add(typeof(PassThroughAttribute), order: 17);
                options.Filters.Add(new FormatFilterAttribute());
            })
            .AddXmlDataContractSerializerFormatters()
            .AddViewLocalization(LanguageViewLocationExpanderFormat.SubFolder);

            services.AddSingleton<PassThroughAttribute>();
            services.AddSingleton<UserNameService>();
            services.AddTransient<ITestService, TestService>();
            

            var applicationEnvironment = services.BuildServiceProvider().GetRequiredService<IApplicationEnvironment>();
            var configurationPath = Path.Combine(applicationEnvironment.ApplicationBasePath, "config.json");

            // Set up configuration sources.
            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile(configurationPath)
                .AddEnvironmentVariables();

            var configuration = configBuilder.Build();

            var diSystem = configuration["DependencyInjection"];
            if (!string.IsNullOrEmpty(diSystem) &&
                diSystem.Equals("AutoFac", StringComparison.OrdinalIgnoreCase))
            {
                _autoFac = true;

                // Create the autofac container
                var builder = new ContainerBuilder();

                // Create the container and use the default application services as a fallback
                builder.Populate(services);

                builder.RegisterModule<MonitoringModule>();

                var container = builder.Build();

                return container.Resolve<IServiceProvider>();
            }
            else
            {
                return services.BuildServiceProvider();
            }
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseStatusCodePages();
            app.UseFileServer();

            if (_autoFac)
            {
                app.UseMiddleware<MonitoringMiddlware>();
            }
            app.UseRequestLocalization();

            app.UseSession();
            app.UseMvc(routes =>
            {
                routes.MapRoute("areaRoute", "{area:exists}/{controller}/{action}");
                routes.MapRoute(
                    "controllerActionRoute",
                    "{controller}/{action}",
                    new { controller = "Home", action = "Index" },
                    constraints: null,
                    dataTokens: new { NameSpace = "default" });

                routes.MapRoute(
                    "controllerRoute",
                    "{controller}",
                    new { controller = "Home" });
            });
        }
    }
}
