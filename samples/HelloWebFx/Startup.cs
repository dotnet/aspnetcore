using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.DependencyInjection.Fallback;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.RequestContainer;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet;

namespace KWebStartup
{
    public class Startup
    {
        public void Configuration(IBuilder app)
        {
            var services = new ServiceCollection();
            services.Add(MvcServices.GetDefaultServices());
            var serviceProvider = services.BuildServiceProvider(app.ServiceProvider);

            var routes = new RouteCollection
            {
                DefaultHandler = new MvcApplication(serviceProvider)
            };

            routes.MapRoute("{controller}/{action}", new { controller = "Home", action = "Index" });
            
            app.UseErrorPage();
            app.UseContainer(serviceProvider);
            app.UseRouter(routes);
            app.UseWelcomePage();
        }       
    }
}