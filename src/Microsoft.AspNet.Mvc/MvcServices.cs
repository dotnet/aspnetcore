using System.Collections.Generic;
using Microsoft.AspNet.ConfigurationModel;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.DependencyInjection.NestedProviders;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.AspNet.Mvc.Razor.Compilation;

namespace Microsoft.AspNet.Mvc
{
    public class MvcServices
    {
        public static IEnumerable<IServiceDescriptor> GetDefaultServices()
        {
            return GetDefaultServices(new Configuration());
        }

        public static IEnumerable<IServiceDescriptor> GetDefaultServices(IConfiguration configuration)
        {
            var describe = new ServiceDescriber(configuration);

            yield return describe.Transient<IControllerFactory, DefaultControllerFactory>();
            yield return describe.Transient<IControllerDescriptorFactory, DefaultControllerDescriptorFactory>();
            yield return describe.Transient<IActionSelector, DefaultActionSelector>();
            yield return describe.Transient<IActionInvokerFactory, ActionInvokerFactory>();
            yield return describe.Transient<IActionResultHelper, ActionResultHelper>();
            yield return describe.Transient<IActionResultFactory, ActionResultFactory>();
            yield return describe.Transient<IParameterDescriptorFactory, DefaultParameterDescriptorFactory>();
            yield return describe.Transient<IControllerAssemblyProvider, AppDomainControllerAssemblyProvider>();
            yield return describe.Transient<IActionDiscoveryConventions, DefaultActionDiscoveryConventions>();

            yield return describe.Instance<IMvcRazorHost>(new MvcRazorHost(typeof(RazorView).FullName));

            yield return describe.Transient<ICompilationService, RoslynCompilationService>();

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
            yield return describe.Transient<IValueProviderFactory, FormValueProviderFactory>();

            yield return describe.Transient<IModelBinder, TypeConverterModelBinder>();
            yield return describe.Transient<IModelBinder, TypeMatchModelBinder>();
            yield return describe.Transient<IModelBinder, GenericModelBinder>();
            yield return describe.Transient<IModelBinder, MutableObjectModelBinder>();
            yield return describe.Transient<IModelBinder, ComplexModelDtoModelBinder>();

            yield return describe.Transient<IInputFormatter, JsonInputFormatter>();

            yield return describe.Transient<INestedProviderManager<FilterProviderContext>, NestedProviderManager<FilterProviderContext>>();
            yield return describe.Transient<INestedProvider<FilterProviderContext>, DefaultFilterProvider>();

            yield return describe.Singleton<IModelValidatorProvider, DataAnnotationsModelValidatorProvider>();
            yield return describe.Singleton<IModelValidatorProvider, DataMemberModelValidatorProvider>();
        }
    }
}
