
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.ConfigurationModel;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;
using Microsoft.Net.Runtime;

namespace MvcSample.Web
{
    public class Startup
    {
        public void Configuration(IBuilder app)
        {
            var configuration = new Configuration();
            var services = MvcServices.GetDefaultServices(configuration);
            var serviceProvider =
                DefaultServiceProvider.Create(app.ServiceProvider, services);

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
