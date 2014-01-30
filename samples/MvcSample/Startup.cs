#if NET45
using System;
using System.IO;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.Owin.FileSystems;
using Owin;

namespace MvcSample
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseErrorPage();

            var serviceProvider = MvcServices.Create();

            // HACK to determine app root.
            string appRoot = Environment.CurrentDirectory;
            while (!String.IsNullOrEmpty(appRoot) && !appRoot.TrimEnd(Path.DirectorySeparatorChar).EndsWith("MvcSample"))
            {
                appRoot = Path.GetDirectoryName(appRoot);
            }

            serviceProvider.AddInstance<IFileSystem>(new PhysicalFileSystem(appRoot));
            serviceProvider.Add<IVirtualFileSystem, VirtualFileSystem>();
            serviceProvider.Add<IMvcRazorHost, MvcRazorHost>();
            serviceProvider.Add<ICompilationService, CscBasedCompilationService>();
            serviceProvider.Add<IRazorCompilationService, RazorCompilationService>();
            serviceProvider.Add<IVirtualPathViewFactory, VirtualPathViewFactory>();
            serviceProvider.Add<IViewEngine, RazorViewEngine>();

            var handler = new MvcHandler(serviceProvider);

            app.RunHttpContext(async context =>
            {
                // Pretending to be routing
                var routeData = new FakeRouteData(context);

                await handler.ExecuteAsync(context, routeData);
            });
        }
    }
}
#endif