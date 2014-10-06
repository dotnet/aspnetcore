// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Description;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.OptionDescriptors;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.AspNet.Mvc.Razor.Compilation;
using Microsoft.AspNet.Mvc.Razor.OptionDescriptors;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Security;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.NestedProviders;
using Microsoft.Framework.OptionsModel;

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

            yield return describe.Transient<IOptionsSetup<MvcOptions>, MvcOptionsSetup>();

            yield return describe.Transient<IControllerFactory, DefaultControllerFactory>();
            yield return describe.Singleton<IControllerActivator, DefaultControllerActivator>();

            yield return describe.Singleton<IActionSelectorDecisionTreeProvider, ActionSelectorDecisionTreeProvider>();
            yield return describe.Scoped<IActionSelector, DefaultActionSelector>();

            // This provider needs access to the per-request services, but might be used many times for a given
            // request.
            yield return describe.Scoped<INestedProvider<ActionConstraintProviderContext>, 
                DefaultActionConstraintProvider>();

            yield return describe.Transient<IActionInvokerFactory, ActionInvokerFactory>();
            yield return describe.Transient<IControllerAssemblyProvider, DefaultControllerAssemblyProvider>();
            yield return describe.Transient<IActionDiscoveryConventions, DefaultActionDiscoveryConventions>();

            // The host is designed to be discarded after consumption and is very inexpensive to initialize.
            yield return describe.Transient<IMvcRazorHost, MvcRazorHost>();

            yield return describe.Singleton<ICompilationService, RoslynCompilationService>();
            yield return describe.Singleton<IRazorCompilationService, RazorCompilationService>();

            // The provider is inexpensive to initialize and provides ViewEngines that may require request
            // specific services.
            yield return describe.Transient<IViewEngineProvider, DefaultViewEngineProvider>();
            yield return describe.Scoped<ICompositeViewEngine, CompositeViewEngine>();
            yield return describe.Singleton<IViewStartProvider, ViewStartProvider>();
            yield return describe.Transient<IRazorView, RazorView>();

            // Transient since the IViewLocationExpanders returned by the instance is cached by view engines.
            yield return describe.Transient<IViewLocationExpanderProvider, DefaultViewLocationExpanderProvider>();
            // Caches view locations that are valid for the lifetime of the application.
            yield return describe.Singleton<IViewLocationCache, DefaultViewLocationCache>();

            yield return describe.Singleton<IRazorPageActivator, RazorPageActivator>();
            // Virtual path view factory needs to stay scoped so views can get get scoped services.
            yield return describe.Scoped<IRazorPageFactory, VirtualPathRazorPageFactory>();
            yield return describe.Singleton<IFileInfoCache, ExpiringFileInfoCache>();

            yield return describe.Transient<INestedProvider<ActionDescriptorProviderContext>,
                                            ControllerActionDescriptorProvider>();
            yield return describe.Transient<INestedProvider<ActionInvokerProviderContext>,
                                            ControllerActionInvokerProvider>();
            yield return describe.Singleton<IActionDescriptorsCollectionProvider,
                DefaultActionDescriptorsCollectionProvider>();

            yield return describe.Transient<IModelMetadataProvider, DataAnnotationsModelMetadataProvider>();
            yield return describe.Scoped<IActionBindingContextProvider, DefaultActionBindingContextProvider>();

            yield return describe.Transient<IInputFormatterSelector, DefaultInputFormatterSelector>();
            yield return describe.Scoped<IInputFormattersProvider, DefaultInputFormattersProvider>();

            yield return describe.Transient<IModelBinderProvider, DefaultModelBindersProvider>();
            yield return describe.Scoped<ICompositeModelBinder, CompositeModelBinder>();
            yield return describe.Transient<IValueProviderFactoryProvider, DefaultValueProviderFactoryProvider>();
            yield return describe.Scoped<ICompositeValueProviderFactory, CompositeValueProviderFactory>();
            yield return describe.Transient<IOutputFormattersProvider, DefaultOutputFormattersProvider>();

            yield return describe.Instance<JsonOutputFormatter>(
                new JsonOutputFormatter(JsonOutputFormatter.CreateDefaultSettings(), indent: false));

            // The IGlobalFilterProvider is used to build the action descriptors (likely once) and so should
            // remain transient to avoid keeping it in memory.
            yield return describe.Transient<IGlobalFilterProvider, DefaultGlobalFilterProvider>();

            yield return describe.Transient<INestedProvider<FilterProviderContext>, DefaultFilterProvider>();

            yield return describe.Transient<IModelValidatorProviderProvider, DefaultModelValidatorProviderProvider>();
            yield return describe.Scoped<ICompositeModelValidatorProvider, CompositeModelValidatorProvider>();

            yield return describe.Scoped<IUrlHelper, UrlHelper>();

            yield return describe.Transient<IViewComponentSelector, DefaultViewComponentSelector>();
            yield return describe.Singleton<IViewComponentActivator, DefaultViewComponentActivator>();
            yield return describe.Transient<IViewComponentInvokerFactory, DefaultViewComponentInvokerFactory>();
            yield return describe.Transient<INestedProvider<ViewComponentInvokerProviderContext>,
                DefaultViewComponentInvokerProvider>();
            yield return describe.Transient<IViewComponentHelper, DefaultViewComponentHelper>();

            yield return describe.Transient<IBodyModelValidator, DefaultBodyModelValidator>();

            yield return describe.Transient<IAuthorizationService, DefaultAuthorizationService>();
            yield return describe.Singleton<IClaimUidExtractor, DefaultClaimUidExtractor>();
            yield return describe.Singleton<AntiForgery, AntiForgery>();
            yield return describe.Singleton<IAntiForgeryAdditionalDataProvider,
                DefaultAntiForgeryAdditionalDataProvider>();

            yield return describe.Singleton<IApiDescriptionGroupCollectionProvider,
                ApiDescriptionGroupCollectionProvider>();
            yield return describe.Transient<INestedProvider<ApiDescriptionProviderContext>,
                DefaultApiDescriptionProvider>();

            yield return
               describe.Describe(
                   typeof(INestedProviderManager<>),
                   typeof(NestedProviderManager<>),
                   implementationInstance: null,
                   lifecycle: LifecycleKind.Transient);

            yield return
                describe.Describe(
                    typeof(INestedProviderManagerAsync<>),
                    typeof(NestedProviderManagerAsync<>),
                    implementationInstance: null,
                    lifecycle: LifecycleKind.Transient);

            yield return describe.Transient<IHtmlHelper, HtmlHelper>();
            yield return
                describe.Describe(
                    typeof(IHtmlHelper<>),
                    typeof(HtmlHelper<>),
                    implementationInstance: null,
                    lifecycle: LifecycleKind.Transient);

            yield return describe.Transient<MvcMarkerService, MvcMarkerService>();
        }
    }
}
