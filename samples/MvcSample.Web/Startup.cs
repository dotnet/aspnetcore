// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
#if DNX451
using System.IO;
using Autofac;
#endif
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Razor;
#if DNX451
using Microsoft.Framework.Configuration;
#endif
using Microsoft.Framework.DependencyInjection;
#if DNX451
using Microsoft.Framework.DependencyInjection.Autofac;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Infrastructure;
#endif
using MvcSample.Web.Filters;
using MvcSample.Web.Services;

namespace MvcSample.Web
{
    public class Startup
    {
#if DNX451
        private bool _autoFac;
#endif

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddCaching();
            services.AddSession();

            services.AddMvc();
            services.AddSingleton<PassThroughAttribute>();
            services.AddSingleton<UserNameService>();
            services.AddTransient<ITestService, TestService>();

            services.ConfigureMvc(options =>
            {
                options.Filters.Add(typeof(PassThroughAttribute), order: 17);
                options.AddXmlDataContractSerializerFormatter();
                options.Filters.Add(new FormatFilterAttribute());
            });

            services.AddMvcLocalization(LanguageViewLocationExpanderOption.SubFolder);

#if DNX451
            // Fully-qualify configuration path to avoid issues in functional tests. Just "config.json" would be fine
            // but Configuration uses CallContextServiceLocator.Locator.ServiceProvider to get IApplicationEnvironment.
            // Functional tests update that service but not in the static provider.
            var applicationEnvironment = services.BuildServiceProvider().GetRequiredService<IApplicationEnvironment>();
            var configurationPath = Path.Combine(applicationEnvironment.ApplicationBasePath, "config.json");

            // Set up configuration sources.
            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile(configurationPath)
                .AddEnvironmentVariables();

            var configuration = configBuilder.Build();

            string diSystem;
            if (configuration.TryGet("DependencyInjection", out diSystem) &&
                diSystem.Equals("AutoFac", StringComparison.OrdinalIgnoreCase))
            {
                _autoFac = true;

                // Create the autofac container
                var builder = new ContainerBuilder();

                // Create the container and use the default application services as a fallback
                AutofacRegistration.Populate(
                    builder,
                    services);

                builder.RegisterModule<MonitoringModule>();

                var container = builder.Build();

                return container.Resolve<IServiceProvider>();
            }
            else
#endif
            {
                return services.BuildServiceProvider();
            }
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseStatusCodePages();
            app.UseFileServer();

#if DNX451
            if (_autoFac)
            {
                app.UseMiddleware<MonitoringMiddlware>();
            }
#endif
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
