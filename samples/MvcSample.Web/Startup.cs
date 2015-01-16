// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.AspNet.Security;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using MvcSample.Web.Filters;
using MvcSample.Web.Services;

#if ASPNET50
using Autofac;
using Microsoft.Framework.DependencyInjection.Autofac;
#endif

namespace MvcSample.Web
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseFileServer();
#if ASPNET50
            // Set up configuration sources.
            var configuration = new Configuration()
                    .AddJsonFile("config.json")
                    .AddEnvironmentVariables();
            string diSystem;

            if (configuration.TryGet("DependencyInjection", out diSystem) &&
                diSystem.Equals("AutoFac", StringComparison.OrdinalIgnoreCase))
            {
                app.UseMiddleware<MonitoringMiddlware>();

                app.UseServices(services =>
                {
                    services.ConfigureAuthorization(auth =>
                    {
                        auth.AddPolicy("CanViewPage", 
                            new AuthorizationPolicyBuilder()
                                .RequiresClaim("Permission", "CanViewPage", "CanViewAnything").Build());
                        auth.AddPolicy("CanViewAnything", 
                            new AuthorizationPolicyBuilder()
                                .RequiresClaim("Permission", "CanViewAnything").Build());
                        // This policy basically requires that the auth type is present
                        var basicPolicy = new AuthorizationPolicyBuilder("Basic").RequiresClaim(ClaimTypes.NameIdentifier);
                        auth.AddPolicy("RequireBasic", basicPolicy.Build());
                    });

                    services.AddMvc();
                    services.AddSingleton<PassThroughAttribute>();
                    services.AddSingleton<UserNameService>();
                    services.AddTransient<ITestService, TestService>();

                    // Setup services with a test AssemblyProvider so that only the
                    // sample's assemblies are loaded. This prevents loading controllers from other assemblies
                    // when the sample is used in the Functional Tests.
                    services.AddTransient<IAssemblyProvider, TestAssemblyProvider<Startup>>();
                    services.Configure<MvcOptions>(options =>
                    {
                        options.Filters.Add(typeof(PassThroughAttribute), order: 17);
                        options.AddXmlDataContractSerializerFormatter();
                        var formatFilter = new FormatFilterAttribute();
                        options.Filters.Add(formatFilter);
                    });
                    services.Configure<RazorViewEngineOptions>(options =>
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
                    services.AddMvc();
                    services.AddSingleton<PassThroughAttribute>();
                    services.AddSingleton<UserNameService>();
                    services.AddTransient<ITestService, TestService>();
                    // Setup services with a test AssemblyProvider so that only the
                    // sample's assemblies are loaded. This prevents loading controllers from other assemblies
                    // when the sample is used in the Functional Tests.
                    services.AddTransient<IAssemblyProvider, TestAssemblyProvider<Startup>>();

                    services.Configure<MvcOptions>(options =>
                    {
                        options.Filters.Add(typeof(PassThroughAttribute), order: 17);
                        options.AddXmlDataContractSerializerFormatter();

                        var formatFilter = new FormatFilterAttribute();
                        options.Filters.Add(formatFilter);
                    });
                });
            }

            app.UseMvc(routes =>
            {
                routes.MapRoute("FormatRoute", "{controller}/{action}/{id}.{format?}");
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
