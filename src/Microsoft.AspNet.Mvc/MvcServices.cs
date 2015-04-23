// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.ActionConstraints;
using Microsoft.AspNet.Mvc.ApiExplorer;
using Microsoft.AspNet.Mvc.ApplicationModels;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Metadata;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.AspNet.Mvc.Razor.Compilation;
using Microsoft.AspNet.Mvc.Razor.Directives;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Mvc.ViewComponents;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc
{
    public class MvcServices
    {
        public static IServiceCollection GetDefaultServices()
        {
            var services = new ServiceCollection();

            // Options and core services.
            services.AddTransient<IConfigureOptions<MvcOptions>, MvcOptionsSetup>();
            services.AddTransient<IConfigureOptions<RazorViewEngineOptions>, RazorViewEngineOptionsSetup>();

            // TryAdd() so functional tests can override this particular service. Test setup runs before this method.
            services.TryAdd(ServiceDescriptor.Transient<IAssemblyProvider, DefaultAssemblyProvider>());

            services.AddTransient<MvcMarkerService, MvcMarkerService>();
            services.AddSingleton<ITypeActivatorCache, DefaultTypeActivatorCache>();
            services.AddScoped(typeof(IScopedInstance<>), typeof(ScopedInstance<>));

            // Core action discovery, filters and action execution.

            // These are consumed only when creating action descriptors, then they can be de-allocated
            services.AddTransient<IControllerTypeProvider, DefaultControllerTypeProvider>();
            services.AddTransient<IControllerModelBuilder, DefaultControllerModelBuilder>();
            services.AddTransient<IActionModelBuilder, DefaultActionModelBuilder>();

            // This has a cache, so it needs to be a singleton
            services.AddSingleton<IControllerFactory, DefaultControllerFactory>();

            services.AddTransient<IControllerActivator, DefaultControllerActivator>();

            // This accesses per-reqest services
            services.AddTransient<IActionInvokerFactory, ActionInvokerFactory>();

            // This provider needs access to the per-request services, but might be used many times for a given
            // request.
            services.AddTransient<IActionConstraintProvider, DefaultActionConstraintProvider>();

            services.AddSingleton<IActionSelectorDecisionTreeProvider, ActionSelectorDecisionTreeProvider>();
            services.AddSingleton<IActionSelector, DefaultActionSelector>();
            services.AddTransient<IControllerActionArgumentBinder, DefaultControllerActionArgumentBinder>();
            services.AddTransient<IObjectModelValidator>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<MvcOptions>>().Options;
                var modelMetadataProvider = serviceProvider.GetRequiredService<IModelMetadataProvider>();
                return new DefaultObjectValidator(options.ValidationExcludeFilters, modelMetadataProvider);
            });

            services.AddTransient<IActionDescriptorProvider, ControllerActionDescriptorProvider>();

            services.AddTransient<IActionInvokerProvider, ControllerActionInvokerProvider>();

            services.AddSingleton<IActionDescriptorsCollectionProvider, DefaultActionDescriptorsCollectionProvider>();

            // The IGlobalFilterProvider is used to build the action descriptors (likely once) and so should
            // remain transient to avoid keeping it in memory.
            services.AddTransient<IGlobalFilterProvider, DefaultGlobalFilterProvider>();
            services.AddTransient<IFilterProvider, DefaultFilterProvider>();

            services.AddTransient<FormatFilter, FormatFilter>();
            services.AddTransient<CorsAuthorizationFilter, CorsAuthorizationFilter>();

            // Dataflow - ModelBinding, Validation and Formatting
            //
            // The DefaultModelMetadataProvider does significant caching and should be a singleton.
            services.AddSingleton<IModelMetadataProvider, DefaultModelMetadataProvider>();
            services.AddTransient<ICompositeMetadataDetailsProvider>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<MvcOptions>>().Options;
                return new DefaultCompositeMetadataDetailsProvider(options.ModelMetadataDetailsProviders);
            });

            services.AddInstance(new JsonOutputFormatter());

            // Razor, Views and runtime compilation

            // The provider is inexpensive to initialize and provides ViewEngines that may require request
            // specific services.
            services.AddScoped<ICompositeViewEngine, CompositeViewEngine>();

            // Caches view locations that are valid for the lifetime of the application.
            services.AddSingleton<IViewLocationCache, DefaultViewLocationCache>();
            services.AddSingleton<ICodeTreeCache>(serviceProvider =>
            {
                var cachedFileProvider = serviceProvider.GetRequiredService<IOptions<RazorViewEngineOptions>>();
                return new DefaultCodeTreeCache(cachedFileProvider.Options.FileProvider);
            });

            // The host is designed to be discarded after consumption and is very inexpensive to initialize.
            services.AddTransient<IMvcRazorHost, MvcRazorHost>();

            // Caches compilation artifacts across the lifetime of the application.
            services.AddSingleton<ICompilerCache, CompilerCache>();

            // This caches compilation related details that are valid across the lifetime of the application
            // and is required to be a singleton.
            services.AddSingleton<ICompilationService, RoslynCompilationService>();

            // Both the compiler cache and roslyn compilation service hold on the compilation related
            // caches. RazorCompilation service is just an adapter service, and it is transient to ensure
            // the IMvcRazorHost dependency does not maintain state.
            services.AddTransient<IRazorCompilationService, RazorCompilationService>();

            // The ViewStartProvider needs to be able to consume scoped instances of IRazorPageFactory
            services.AddScoped<IViewStartProvider, ViewStartProvider>();
            services.AddTransient<IRazorViewFactory, RazorViewFactory>();
            services.AddSingleton<IRazorPageActivator, RazorPageActivator>();

            // Virtual path view factory needs to stay scoped so views can get get scoped services.
            services.AddScoped<IRazorPageFactory, VirtualPathRazorPageFactory>();

            // View and rendering helpers
            services.AddTransient<IHtmlHelper, HtmlHelper>();
            services.AddTransient(typeof(IHtmlHelper<>), typeof(HtmlHelper<>));
            services.AddScoped<IUrlHelper, UrlHelper>();

            // Only want one ITagHelperActivator so it can cache Type activation information. Types won't conflict.
            services.AddSingleton<ITagHelperActivator, DefaultTagHelperActivator>();

            // Consumed by the Cache tag helper to cache results across the lifetime of the application.
            services.AddSingleton<IMemoryCache, MemoryCache>();

            // DefaultHtmlGenerator is pretty much stateless but depends on Scoped services such as IUrlHelper and
            // IActionBindingContextProvider. Therefore it too is scoped.
            services.AddTransient<IHtmlGenerator, DefaultHtmlGenerator>();

            // These do caching so they should stay singleton
            services.AddSingleton<IViewComponentSelector, DefaultViewComponentSelector>();
            services.AddSingleton<IViewComponentActivator, DefaultViewComponentActivator>();
            services.AddSingleton<IViewComponentDescriptorCollectionProvider,
                DefaultViewComponentDescriptorCollectionProvider>();

            services.AddTransient<IViewComponentDescriptorProvider, DefaultViewComponentDescriptorProvider>();
            services.AddTransient<IViewComponentInvokerFactory, DefaultViewComponentInvokerFactory>();
            services.AddTransient<IViewComponentHelper, DefaultViewComponentHelper>();

            // Security and Authorization
            services.AddSingleton<IClaimUidExtractor, DefaultClaimUidExtractor>();
            services.AddSingleton<AntiForgery, AntiForgery>();
            services.AddSingleton<IAntiForgeryAdditionalDataProvider, DefaultAntiForgeryAdditionalDataProvider>();

            // Api Description
            services.AddSingleton<IApiDescriptionGroupCollectionProvider, ApiDescriptionGroupCollectionProvider>();
            services.AddTransient<IApiDescriptionProvider, DefaultApiDescriptionProvider>();

            // Temp Data
            services.AddScoped<ITempDataDictionary, TempDataDictionary>();
            // This does caching so it should stay singleton
            services.AddSingleton<ITempDataProvider, SessionStateTempDataProvider>();

            return services;
        }
    }
}
