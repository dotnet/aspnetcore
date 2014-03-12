#if NET45
using Autofac;
using System;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.ConfigurationModel;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.DependencyInjection.Autofac;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;
using Microsoft.Net.Runtime;
using Owin;

namespace MvcSample
{
    public class Startup
    {
        private IServiceProvider _serviceProvider;

        public Startup(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Configuration(IAppBuilder app)
        {
            app.UseErrorPage();

            // Temporary bridge from katana to Owin
            app.UseBuilder(ConfigureMvc);
        }

        private void ConfigureMvc(IBuilder builder)
        {
            var containerBuilder = new ContainerBuilder();
            var services = MvcServices.GetDefaultServices();

            AutofacRegistration.Populate(containerBuilder, _serviceProvider, services);
            containerBuilder.RegisterInstance<PassThroughAttribute>(new PassThroughAttribute());

            var serviceProvider = containerBuilder.Build().Resolve<IServiceProvider>();

            var routes = new RouteCollection()
            {
                DefaultHandler = new MvcApplication(serviceProvider),
            };

            routes.MapRoute(
                "{controller}/{action}",
                new { controller = "Home", action = "Index" });

            routes.MapRoute(
                "{controller}",
                new { controller = "Home" });

            builder.UseRouter(routes);
        }
    }
}
#endif
