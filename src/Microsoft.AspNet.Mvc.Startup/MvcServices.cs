using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.Owin.FileSystems;

namespace Microsoft.AspNet.Mvc.Startup
{
    public static class MvcServices
    {
        public static ServiceProvider Create(string appRoot)
        {
            var services = new ServiceProvider();
            services.Add<IControllerFactory, DefaultControllerFactory>();
            services.Add<IActionInvokerFactory, ActionInvokerFactory>();
            services.Add<IActionResultHelper, ActionResultHelper>();
            services.Add<IActionResultFactory, ActionResultFactory>();
            services.Add<IActionDescriptorProvider, ActionDescriptorProvider>();
            services.Add<IActionInvokerProvider, ActionInvokerProvider>();

            services.AddInstance<IFileSystem>(new PhysicalFileSystem(appRoot));
            services.AddInstance<IMvcRazorHost>(new MvcRazorHost("Microsoft.AspNet.Mvc.Razor.RazorView<dynamic>"));
            #if NET45
            services.Add<ICompilationService, CscBasedCompilationService>();
            #endif
            services.Add<IRazorCompilationService, RazorCompilationService>();
            services.Add<IVirtualPathViewFactory, VirtualPathViewFactory>();
            services.Add<IViewEngine, RazorViewEngine>();

            return services;
        }
    }
}
