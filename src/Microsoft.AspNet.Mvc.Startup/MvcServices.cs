using System;
using System.Collections.Generic;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet.Mvc.Startup
{
    public class MvcServices
    {
        private object _lock = new object();

        private List<Type> _typesToFinalize = new List<Type>();

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

        private void Add<T, U>() where U : T
        {
            Services.Add<T, U>();
        }

        private void AddInstance<T>(object instance)
        {
            Services.AddInstance<T>(instance);
        }
    }
}
