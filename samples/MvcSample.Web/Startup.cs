
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.ConfigurationModel;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Template;
using Microsoft.Net.Runtime;

namespace MvcSample.Web
{
    public class Startup
    {
        private readonly IApplicationEnvironment _env;

        public Startup(IApplicationEnvironment env)
        {
            _env = env;
        }

        public void Configuration(IBuilder app)
        {
            var configuration = new Configuration();
            var serviceProvider = new ServiceProvider().Add(MvcServices.GetDefaultServices(configuration, _env));

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

            app.UseRouter(routes);
        }
    }
}
