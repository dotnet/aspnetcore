using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using MvcSample.Web.Filters;
using MvcSample.Web.Services;

#if ASPNET50 
using Autofac;
using Microsoft.Framework.DependencyInjection.Autofac;
using Microsoft.Framework.OptionsModel;
#endif

namespace MvcSample.Web
{
    public class Startup
    {
        public void Configure(IBuilder app)
        {
            app.UseFileServer();
#if ASPNET50
            var configuration = new Configuration()
                                        .AddJsonFile(@"App_Data\config.json")
                                        .AddEnvironmentVariables();
            string diSystem;

            if (configuration.TryGet("DependencyInjection", out diSystem) &&
                diSystem.Equals("AutoFac", StringComparison.OrdinalIgnoreCase))
            {
                app.UseMiddleware<MonitoringMiddlware>();

                var services = new ServiceCollection();

                services.AddMvc();
                services.AddSingleton<PassThroughAttribute>();
                services.AddSingleton<UserNameService>();
                services.AddTransient<ITestService, TestService>();
                services.Add(OptionsServices.GetDefaultServices());
                // Setup services with a test AssemblyProvider so that only the
                // sample's assemblies are loaded. This prevents loading controllers from other assemblies
                // when the sample is used in the Functional Tests.
                services.AddTransient<IControllerAssemblyProvider, TestAssemblyProvider<Startup>>();

                // Create the autofac container 
                ContainerBuilder builder = new ContainerBuilder();

                // Create the container and use the default application services as a fallback 
                AutofacRegistration.Populate(
                    builder,
                    services,
                    fallbackServiceProvider: app.ApplicationServices);

                builder.RegisterModule<MonitoringModule>();

                IContainer container = builder.Build();

                app.UseServices(container.Resolve<IServiceProvider>());
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
                    services.AddTransient<IControllerAssemblyProvider, TestAssemblyProvider<Startup>>();
                });
            }

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
