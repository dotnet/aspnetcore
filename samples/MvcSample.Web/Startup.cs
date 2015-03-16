// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNX451
using System;
using System.IO;
using Autofac;
#endif
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Razor;
#if DNX451
using Microsoft.Framework.ConfigurationModel;
#endif
using Microsoft.Framework.DependencyInjection;
#if DNX451
using Microsoft.Framework.DependencyInjection.Autofac;
using Microsoft.Framework.Runtime;
#endif
using MvcSample.Web.Filters;
using MvcSample.Web.Services;


namespace MvcSample.Web
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseStatusCodePages();
            app.UseFileServer();

#if DNX451
            // Fully-qualify configuration path to avoid issues in functional tests. Just "config.json" would be fine
            // but Configuration uses CallContextServiceLocator.Locator.ServiceProvider to get IApplicationEnvironment.
            // Functional tests update that service but not in the static provider.
            var applicationEnvironment = app.ApplicationServices.GetRequiredService<IApplicationEnvironment>();
            var configurationPath = Path.Combine(applicationEnvironment.ApplicationBasePath, "config.json");

            // Set up configuration sources.
            var configuration = new Configuration()
                .AddJsonFile(configurationPath)
                .AddEnvironmentVariables();

            string diSystem;
            if (configuration.TryGet("DependencyInjection", out diSystem) &&
                diSystem.Equals("AutoFac", StringComparison.OrdinalIgnoreCase))
            {
                app.UseMiddleware<MonitoringMiddlware>();

                app.UseServices(services =>
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
                    services.ConfigureRazorViewEngine(options =>
                    {
                        var expander = new LanguageViewLocationExpander(
                            context => context.HttpContext.Request.Query["language"]);
                        options.ViewLocationExpanders.Insert(0, expander);
                    });

                    // Create the autofac container
                    ContainerBuilder builder = new ContainerBuilder();

                    // Create the container and use the default application services as a fallback
                    AutofacRegistration.Populate(
                        builder,
                        services);

                    builder.RegisterModule<MonitoringModule>();

                    IContainer container = builder.Build();

                    return container.Resolve<IServiceProvider>();
                });
            }
            else
#endif
            {
                app.UseServices(services =>
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
                });
            }

            app.UseInMemorySession();
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
