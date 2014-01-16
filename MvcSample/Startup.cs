using System;
using System.Threading.Tasks;
using Microsoft.AspNet.CoreServices;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(MvcSample.Startup))]

namespace MvcSample
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseErrorPage();

            var serviceProvider = MvcServices.Create();
            serviceProvider.AddInstance<IVirtualPathFactory>(new MetadataVirtualPathProvider(GetType().Assembly));
            serviceProvider.Add<IViewEngine, RazorViewEngine>();
            
            var handler = new MvcHandler(serviceProvider);
            
            app.Run(async context =>
            {
                // Pretending to be routing
                var routeData = new FakeRouteData(context);

                await handler.ExecuteAsync(context, routeData);
            });
        }
    }
}
