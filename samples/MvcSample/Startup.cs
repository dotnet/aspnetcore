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
        private readonly IServiceProvider _serviceProvider;
        private readonly IApplicationEnvironment _env;

        public Startup(IServiceProvider serviceProvider,
                       IApplicationEnvironment env)
        {
            _serviceProvider = serviceProvider;
            _env = env;
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
            var services = MvcServices.GetDefaultServices(configuration, _env);
            var serviceProvider = new ServiceProvider().Add(services);

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
