using System;
using System.Threading.Tasks;
using Microsoft.AspNet.CoreServices;
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

            string appRoot = Environment.GetEnvironmentVariable("WEB_ROOT") ??
                             AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            var serviceProvider = MvcServices.Create();
            var fileSystem = new PhysicalFileSystem(appRoot);
            serviceProvider.AddInstance<IFileSystem>(new VirtualFileSystem(fileSystem));
            serviceProvider.AddInstance<ICompilationService>(new RazorCompilationService(new CscBasedCompilationService()));
            serviceProvider.Add<IVirtualPathViewFactory, VirtualPathViewFactory>();
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
