#if NET45
using System;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.ConfigurationModel;
using Microsoft.AspNet.DependencyInjection;
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
            var configuration = new Configuration();
            var services = MvcServices.GetDefaultServices(configuration);
            var serviceProvider = new ServiceProvider(_serviceProvider).Add(services);

            serviceProvider.AddInstance<PassThroughAttribute>(new PassThroughAttribute());

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
