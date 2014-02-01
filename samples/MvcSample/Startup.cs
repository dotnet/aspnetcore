#if NET45
using System;
using System.IO;
using Microsoft.AspNet.Abstractions;
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

            // Temporary bridge from katana to Owin
            app.UseBuilder(ConfigureMvc);
        }

        private void ConfigureMvc(IBuilder builder)
        {
            var serviceProvider = MvcServices.Create();

            // HACK appbase doesn't seem to work. When in VS we're pointing at bin\Debug\Net45, so move up 3 directories
            string appRoot = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", "..", ".."));

            serviceProvider.AddInstance<IFileSystem>(new PhysicalFileSystem(appRoot));
            serviceProvider.Add<IVirtualFileSystem, VirtualFileSystem>();
            serviceProvider.AddInstance<IMvcRazorHost>(new MvcRazorHost("Microsoft.AspNet.Mvc.Razor.RazorView<dynamic>"));
            serviceProvider.Add<ICompilationService, CscBasedCompilationService>();
            serviceProvider.Add<IRazorCompilationService, RazorCompilationService>();
            serviceProvider.Add<IVirtualPathViewFactory, VirtualPathViewFactory>();
            serviceProvider.Add<IViewEngine, RazorViewEngine>();

            var handler = new MvcHandler(serviceProvider);

            builder.Run(async context =>
            {
                // Pretending to be routing
                var routeData = new FakeRouteData(context);

                await handler.ExecuteAsync(context, routeData);
            });
        }
    }
}
#endif