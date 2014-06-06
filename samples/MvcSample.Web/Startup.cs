using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using MvcSample.Web.Filters;
using MvcSample.Web.Services;

#if NET45 
using Autofac;
using Microsoft.Framework.DependencyInjection.Autofac;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.OptionsModel;
#endif

namespace MvcSample.Web
{
    public class Startup
    {
        public void Configure(IBuilder app)
        {
#if NET45
            var configuration = new Configuration()
                                    .AddJsonFile(@"App_Data\config.json")
                                    .AddEnvironmentVariables();

            string diSystem;

            if (configuration.TryGet("DependencyInjection", out diSystem) && 
                diSystem.Equals("AutoFac", StringComparison.OrdinalIgnoreCase))
            {
                var services = new ServiceCollection();

                services.AddMvc();
                services.AddSingleton<PassThroughAttribute>();
                services.AddSingleton<UserNameService>();
                services.AddTransient<ITestService, TestService>();                
                services.Add(OptionsServices.GetDefaultServices());

                // Create the autofac container 
                ContainerBuilder builder = new ContainerBuilder();

                // Create the container and use the default application services as a fallback 
                AutofacRegistration.Populate(
                    builder,
                    services,
                    fallbackServiceProvider: app.ApplicationServices);

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
                });
            }

            app.UseMvc(routes =>
            {
                routes.MapRoute("areaRoute", "{area:exists}/{controller}/{action}");

                routes.MapRoute(
                    "controllerActionRoute",
                    "{controller}/{action}",
                    new { controller = "Home", action = "Index" });

                routes.MapRoute(
                    "controllerRoute",
                    "{controller}",
                    new { controller = "Home" });
            });
        }
    }
}
