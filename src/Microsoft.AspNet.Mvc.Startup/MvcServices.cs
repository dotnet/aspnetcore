using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet.Mvc.Startup
{
    public class MvcServices
    {
        public ServiceProvider Services { get; private set; }

        public MvcServices(string appRoot)
        {
            Services = new ServiceProvider();

            Add<IControllerFactory, DefaultControllerFactory>();
            Add<IActionInvokerFactory, ActionInvokerFactory>();
            Add<IActionResultHelper, ActionResultHelper>();
            Add<IActionResultFactory, ActionResultFactory>();
            Add<IRouteContextProvider, ControllerActionBasedRouteContextProvider>();
            Add<IActionInvokerProvider, ActionInvokerProvider>();

            // need singleton support here.
            // need a design for immutable caches at startup
            var provider = new DefaultControllerDescriptorProvider(new AppDomainControllerAssemblyProvider());
            provider.FinalizeSetup();

            AddInstance<IControllerDescriptorProvider>(provider);
            AddInstance<IFileSystem>(new PhysicalFileSystem(appRoot));
            AddInstance<IMvcRazorHost>(new MvcRazorHost(typeof(RazorView).FullName));

#if NET45
            Add<ICompilationService, CscBasedCompilationService>();
#endif
            Add<IRazorCompilationService, RazorCompilationService>();
            Add<IVirtualPathViewFactory, VirtualPathViewFactory>();
            Add<IViewEngine, RazorViewEngine>();
        }

        private void Add<T, TU>() where TU : T
        {
            Services.Add<T, TU>();
        }

        private void AddInstance<T>(object instance)
        {
            Services.AddInstance<T>(instance);
        }
    }
}
