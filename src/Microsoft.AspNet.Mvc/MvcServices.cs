using System;
using System.Collections.Generic;
using Microsoft.AspNet.ConfigurationModel;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.DependencyInjection.NestedProviders;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.Mvc
{
    public class MvcServices
    {
        public static IEnumerable<IServiceDescriptor> GetDefaultServices(IConfiguration configuration,
                                                                         IApplicationEnvironment env)
        {
            var describe = new ServiceDescriber(configuration);

            yield return describe.Transient<IControllerFactory, DefaultControllerFactory>();
            yield return describe.Transient<IControllerDescriptorFactory, DefaultControllerDescriptorFactory>();
            yield return describe.Transient<IActionSelector, DefaultActionSelector>();
            yield return describe.Transient<IActionInvokerFactory, ActionInvokerFactory>();
            yield return describe.Transient<IActionResultHelper, ActionResultHelper>();
            yield return describe.Transient<IActionResultFactory, ActionResultFactory>();
            yield return describe.Transient<IParameterDescriptorFactory, DefaultParameterDescriptorFactory>();
            yield return describe.Transient<IValueProviderFactory, RouteValueValueProviderFactory>();
            yield return describe.Transient<IValueProviderFactory, QueryStringValueProviderFactory>();
            yield return describe.Transient<IControllerAssemblyProvider, AppDomainControllerAssemblyProvider>();
            yield return describe.Transient<IActionDiscoveryConventions, DefaultActionDiscoveryConventions>();
            yield return describe.Instance<IFileSystem>(new PhysicalFileSystem(env.ApplicationBasePath));

            yield return describe.Instance<IMvcRazorHost>(new MvcRazorHost(typeof(RazorView).FullName));

#if NET45
            // TODO: Container chaining to flow services from the host to this container

            yield return describe.Transient<ICompilationService, CscBasedCompilationService>();

            // TODO: Make this work like normal when we get container chaining
            // TODO: Update this when we have the new host services
            // yield return describe.Instance<ICompilationService>(new RoslynCompilationService(hostServiceProvider));
#endif
            yield return describe.Transient<IRazorCompilationService, RazorCompilationService>();
            yield return describe.Transient<IVirtualPathViewFactory, VirtualPathViewFactory>();
            yield return describe.Transient<IViewEngine, RazorViewEngine>();

            // This is temporary until DI has some magic for it
            yield return describe.Transient<INestedProviderManager<ActionDescriptorProviderContext>,
                                            NestedProviderManager<ActionDescriptorProviderContext>>();
            yield return describe.Transient<INestedProviderManager<ActionInvokerProviderContext>,
                                            NestedProviderManager<ActionInvokerProviderContext>>();
            yield return describe.Transient<INestedProvider<ActionDescriptorProviderContext>,
                                            ReflectedActionDescriptorProvider>();
            yield return describe.Transient<INestedProvider<ActionInvokerProviderContext>,
                                            ReflectedActionInvokerProvider>();

            yield return describe.Transient<IModelMetadataProvider, DataAnnotationsModelMetadataProvider>();
            yield return describe.Transient<IActionBindingContextProvider, DefaultActionBindingContextProvider>();

            yield return describe.Transient<IValueProviderFactory, RouteValueValueProviderFactory>();
            yield return describe.Transient<IValueProviderFactory, QueryStringValueProviderFactory>();

            yield return describe.Transient<IModelBinder, TypeConverterModelBinder>();
            yield return describe.Transient<IModelBinder, TypeMatchModelBinder>();
            yield return describe.Transient<IModelBinder, GenericModelBinder>();
            yield return describe.Transient<IModelBinder, MutableObjectModelBinder>();
            yield return describe.Transient<IModelBinder, ComplexModelDtoModelBinder>();

            yield return describe.Transient<INestedProviderManager<FilterProviderContext>, NestedProviderManager<FilterProviderContext>>();
            yield return describe.Transient<INestedProvider<FilterProviderContext>, DefaultFilterProvider>();

            yield return describe.Transient<IInputFormatter, JsonInputFormatter>();
        }
    }
}
