using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.Mvc.Razor;

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

            AddAndRegisterForFinalization<ControllerCache, DefaultControllerCache>();
            AddAndRegisterForFinalization<IControllerFactory, DefaultControllerFactory>();
            AddAndRegisterForFinalization<IActionInvokerFactory, ActionInvokerFactory>();
            AddAndRegisterForFinalization<IActionResultHelper, ActionResultHelper>();
            AddAndRegisterForFinalization<IActionResultFactory, ActionResultFactory>();
            AddAndRegisterForFinalization<IActionDescriptorProvider, ActionDescriptorProvider>();
            AddAndRegisterForFinalization<IActionInvokerProvider, ActionInvokerProvider>();

            // need singleton support here.
            // AddAndRegisterForFinalization<SkipAssemblies, DefaultSkipAssemblies>();
            AddInstanceAndRegisterForFinalization<ControllerCache>(new DefaultControllerCache(new DefaultSkipAssemblies()));
            AddInstanceAndRegisterForFinalization<IFileSystem>(new PhysicalFileSystem(appRoot));
            AddInstanceAndRegisterForFinalization<IMvcRazorHost>(new MvcRazorHost("Microsoft.AspNet.Mvc.Razor.RazorView<dynamic>"));

#if NET45
            AddAndRegisterForFinalization<ICompilationService, CscBasedCompilationService>();
#endif
            AddAndRegisterForFinalization<IRazorCompilationService, RazorCompilationService>();
            AddAndRegisterForFinalization<IVirtualPathViewFactory, VirtualPathViewFactory>();
            AddAndRegisterForFinalization<IViewEngine, RazorViewEngine>();
        }

        public void AddAndRegisterForFinalization<T, U>() where U : T
        {
            Services.Add<T, U>();
#if NET45
            if (typeof(IFinalizeSetup).IsAssignableFrom(typeof(U)))
#else
            if (typeof(IFinalizeSetup).GetTypeInfo().IsAssignableFrom(typeof(U).GetTypeInfo()))
#endif
            {
                _typesToFinalize.Add(typeof(T));
            }
        }

        public void AddInstanceAndRegisterForFinalization<T>(object instance)
        {
            Services.AddInstance<T>(instance);

            if ((instance as IFinalizeSetup) != null)
            {
                _typesToFinalize.Add(typeof(T));
            }
        }

        public void Finalize()
        {
            if (_typesToFinalize == null)
            {
                return;
            }

            // We want to lock around here so finalization happens just once.
            // This is not a code intended to be used during request, so the lock is just a safety precaution.
            lock (_lock)
            {
                if (_typesToFinalize == null)
                {
                    return;
                }

                foreach (var markerType in _typesToFinalize)
                {
                    var services = this.Services.GetService(markerType);

                    var serviceToFinalize = services as IFinalizeSetup;

                    if (serviceToFinalize != null)
                    {
                        serviceToFinalize.FinalizeSetup();
                    }
                    else
                    {
                        var setOfServices = services as IEnumerable;

                        if (setOfServices != null)
                        {
                            foreach (var service in setOfServices.OfType<IFinalizeSetup>())
                            {
                                service.FinalizeSetup();
                            }
                        }
                    }
                }

                _typesToFinalize = null;
            }
        }
    }
}
