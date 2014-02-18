using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Razor;

namespace Microsoft.AspNet.Mvc.Startup
{
    public class MvcServices
    {
        public ServiceProvider Services { get; private set; }

        public MvcServices(string appRoot)
        {
            Services = new ServiceProvider();

            Add<IControllerFactory, DefaultControllerFactory>();
            Add<IControllerDescriptorFactory, DefaultControllerDescriptorFactory>();
            Add<IActionSelector, DefaultActionSelector>();
            Add<IActionInvokerFactory, ActionInvokerFactory>();
            Add<IActionResultHelper, ActionResultHelper>();
            Add<IActionResultFactory, ActionResultFactory>();
            Add<IActionDescriptorProvider, TypeMethodBasedActionDescriptorProvider>();
            Add<IParameterDescriptorFactory, DefaultParameterDescriptorFactory>();
            Add<IValueProviderFactory, RouteValueValueProviderFactory>();
            Add<IValueProviderFactory, QueryStringValueProviderFactory>();
            Add<IActionInvokerProvider, ActionInvokerProvider>();
            Add<IControllerAssemblyProvider, AppDomainControllerAssemblyProvider>();
            Add<IActionDiscoveryConventions, DefaultActionDiscoveryConventions>();

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
