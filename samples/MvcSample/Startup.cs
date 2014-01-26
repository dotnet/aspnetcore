#if NET45
using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
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

            string appRoot = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;

            var fileSystem = new PhysicalFileSystem(appRoot);
            serviceProvider.AddInstance<IFileSystem>(new VirtualFileSystem(fileSystem));
            serviceProvider.AddInstance<ICompilationService>(new RazorCompilationService(new CscBasedCompilationService()));
            serviceProvider.Add<IVirtualPathViewFactory, VirtualPathViewFactory>();
            serviceProvider.Add<IViewEngine, RazorViewEngine>();

            var handler = new MvcHandler(serviceProvider);

            app.Run(async context =>
            {
                var httpContext = new OwinHttpContext(context);

                // Pretending to be routing
                var routeData = new FakeRouteData(httpContext);

                await handler.ExecuteAsync(httpContext, routeData);
            });
        }
    }
}
#endif