// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc;
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
using Microsoft.AspNet.Routing;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.Framework.DependencyInjection
{
    public static class MvcServiceCollectionExtensions
    {
        public static IServiceCollection AddMvc([NotNull] this IServiceCollection services)
        {
            ConfigureDefaultServices(services);

            // Options and core services.
            services.TryAdd(ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, MvcOptionsSetup>());
            services.TryAdd(
                ServiceDescriptor.Transient<IConfigureOptions<RazorViewEngineOptions>, RazorViewEngineOptionsSetup>());

            services.TryAdd(ServiceDescriptor.Transient<IAssemblyProvider, DefaultAssemblyProvider>());

            services.TryAdd(ServiceDescriptor.Transient<MvcMarkerService, MvcMarkerService>());
            services.TryAdd((ServiceDescriptor.Singleton<ITypeActivatorCache, DefaultTypeActivatorCache>()));
            services.TryAdd(ServiceDescriptor.Scoped(typeof(IScopedInstance<>), typeof(ScopedInstance<>)));

            // Core action discovery, filters and action execution.

            // These are consumed only when creating action descriptors, then they can be de-allocated
            services.TryAdd(ServiceDescriptor.Transient<IControllerTypeProvider, DefaultControllerTypeProvider>());
            services.TryAdd(ServiceDescriptor.Transient<IControllerModelBuilder, DefaultControllerModelBuilder>());
            services.TryAdd(ServiceDescriptor.Transient<IActionModelBuilder, DefaultActionModelBuilder>());

            // This has a cache, so it needs to be a singleton
            services.TryAdd(ServiceDescriptor.Singleton<IControllerFactory, DefaultControllerFactory>());

            services.TryAdd(ServiceDescriptor.Transient<IControllerActivator, DefaultControllerActivator>());

            // This accesses per-request services
            services.TryAdd(ServiceDescriptor.Transient<IActionInvokerFactory, ActionInvokerFactory>());

            // This provider needs access to the per-request services, but might be used many times for a given
            // request.
            services.TryAdd(ServiceDescriptor.Transient<IActionConstraintProvider, DefaultActionConstraintProvider>());

            services.TryAdd(ServiceDescriptor
                .Singleton<IActionSelectorDecisionTreeProvider, ActionSelectorDecisionTreeProvider>());
            services.TryAdd(ServiceDescriptor.Singleton<IActionSelector, DefaultActionSelector>());
            services.TryAdd(ServiceDescriptor
                .Transient<IControllerActionArgumentBinder, DefaultControllerActionArgumentBinder>());
            services.TryAdd(ServiceDescriptor.Transient<IObjectModelValidator>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<MvcOptions>>().Options;
                var modelMetadataProvider = serviceProvider.GetRequiredService<IModelMetadataProvider>();
                return new DefaultObjectValidator(options.ValidationExcludeFilters, modelMetadataProvider);
            }));

            services.TryAdd(ServiceDescriptor
                .Transient<IActionDescriptorProvider, ControllerActionDescriptorProvider>());

            services.TryAdd(ServiceDescriptor.Transient<IActionInvokerProvider, ControllerActionInvokerProvider>());

            services.TryAdd(ServiceDescriptor
                .Singleton<IActionDescriptorsCollectionProvider, DefaultActionDescriptorsCollectionProvider>());

            // The IGlobalFilterProvider is used to build the action descriptors (likely once) and so should
            // remain transient to avoid keeping it in memory.
            services.TryAdd(ServiceDescriptor.Transient<IGlobalFilterProvider, DefaultGlobalFilterProvider>());
            services.TryAdd(ServiceDescriptor.Transient<IFilterProvider, DefaultFilterProvider>());

            services.TryAdd(ServiceDescriptor.Transient<FormatFilter, FormatFilter>());
            services.TryAdd(ServiceDescriptor.Transient<CorsAuthorizationFilter, CorsAuthorizationFilter>());

            // Dataflow - ModelBinding, Validation and Formatting
            //
            // The DefaultModelMetadataProvider does significant caching and should be a singleton.
            services.TryAdd(ServiceDescriptor.Singleton<IModelMetadataProvider, DefaultModelMetadataProvider>());
            services.TryAdd(ServiceDescriptor.Transient<ICompositeMetadataDetailsProvider>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<MvcOptions>>().Options;
                return new DefaultCompositeMetadataDetailsProvider(options.ModelMetadataDetailsProviders);
            }));

            // JsonOutputFormatter should use the SerializerSettings on MvcOptions
            services.TryAdd(ServiceDescriptor.Singleton<JsonOutputFormatter>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<MvcOptions>>().Options;
                return new JsonOutputFormatter(options.SerializerSettings);
            }));

            // Razor, Views and runtime compilation

            // The provider is inexpensive to initialize and provides ViewEngines that may require request
            // specific services.
            services.TryAdd(ServiceDescriptor.Scoped<ICompositeViewEngine, CompositeViewEngine>());

            // Caches view locations that are valid for the lifetime of the application.
            services.TryAdd(ServiceDescriptor.Singleton<IViewLocationCache, DefaultViewLocationCache>());
            services.TryAdd(ServiceDescriptor.Singleton<IChunkTreeCache>(serviceProvider =>
            {
                var cachedFileProvider = serviceProvider.GetRequiredService<IOptions<RazorViewEngineOptions>>();
                return new DefaultChunkTreeCache(cachedFileProvider.Options.FileProvider);
            }));

            // The host is designed to be discarded after consumption and is very inexpensive to initialize.
            services.TryAdd(ServiceDescriptor.Transient<IMvcRazorHost, MvcRazorHost>());

            // Caches compilation artifacts across the lifetime of the application.
            services.TryAdd(ServiceDescriptor.Singleton<ICompilerCache, CompilerCache>());

            // This caches compilation related details that are valid across the lifetime of the application
            // and is required to be a singleton.
            services.TryAdd(ServiceDescriptor.Singleton<ICompilationService, RoslynCompilationService>());

            // Both the compiler cache and roslyn compilation service hold on the compilation related
            // caches. RazorCompilation service is just an adapter service, and it is transient to ensure
            // the IMvcRazorHost dependency does not maintain state.
            services.TryAdd(ServiceDescriptor.Transient<IRazorCompilationService, RazorCompilationService>());

            // The ViewStartProvider needs to be able to consume scoped instances of IRazorPageFactory
            services.TryAdd(ServiceDescriptor.Scoped<IViewStartProvider, ViewStartProvider>());
            services.TryAdd(ServiceDescriptor.Transient<IRazorViewFactory, RazorViewFactory>());
            services.TryAdd(ServiceDescriptor.Singleton<IRazorPageActivator, RazorPageActivator>());

            // Virtual path view factory needs to stay scoped so views can get get scoped services.
            services.TryAdd(ServiceDescriptor.Scoped<IRazorPageFactory, VirtualPathRazorPageFactory>());

            // View and rendering helpers
            services.TryAdd(ServiceDescriptor.Transient<IHtmlHelper, HtmlHelper>());
            services.TryAdd(ServiceDescriptor.Transient(typeof(IHtmlHelper<>), typeof(HtmlHelper<>)));
            services.TryAdd(ServiceDescriptor.Transient<IJsonHelper, JsonHelper>());
            services.TryAdd(ServiceDescriptor.Scoped<IUrlHelper, UrlHelper>());

            // Only want one ITagHelperActivator so it can cache Type activation information. Types won't conflict.
            services.TryAdd(ServiceDescriptor.Singleton<ITagHelperActivator, DefaultTagHelperActivator>());

            // Consumed by the Cache tag helper to cache results across the lifetime of the application.
            services.TryAdd(ServiceDescriptor.Singleton<IMemoryCache, MemoryCache>());

            // DefaultHtmlGenerator is pretty much stateless but depends on Scoped services such as IUrlHelper and
            // IActionBindingContextProvider. Therefore it too is scoped.
            services.TryAdd(ServiceDescriptor.Transient<IHtmlGenerator, DefaultHtmlGenerator>());

            // These do caching so they should stay singleton
            services.TryAdd(ServiceDescriptor.Singleton<IViewComponentSelector, DefaultViewComponentSelector>());
            services.TryAdd(ServiceDescriptor.Singleton<IViewComponentActivator, DefaultViewComponentActivator>());
            services.TryAdd(ServiceDescriptor.Singleton<
                IViewComponentDescriptorCollectionProvider,
                DefaultViewComponentDescriptorCollectionProvider>());

            services.TryAdd(ServiceDescriptor
                .Transient<IViewComponentDescriptorProvider, DefaultViewComponentDescriptorProvider>());
            services.TryAdd(ServiceDescriptor
                .Transient<IViewComponentInvokerFactory, DefaultViewComponentInvokerFactory>());
            services.TryAdd(ServiceDescriptor.Transient<IViewComponentHelper, DefaultViewComponentHelper>());

            // Security and Authorization
            services.TryAdd(ServiceDescriptor.Singleton<IClaimUidExtractor, DefaultClaimUidExtractor>());
            services.TryAdd(ServiceDescriptor.Singleton<AntiForgery, AntiForgery>());
            services.TryAdd(ServiceDescriptor
                .Singleton<IAntiForgeryAdditionalDataProvider, DefaultAntiForgeryAdditionalDataProvider>());

            // Api Description
            services.TryAdd(ServiceDescriptor
                .Singleton<IApiDescriptionGroupCollectionProvider, ApiDescriptionGroupCollectionProvider>());
            services.TryAdd(ServiceDescriptor.Transient<IApiDescriptionProvider, DefaultApiDescriptionProvider>());

            // Temp Data
            services.TryAdd(ServiceDescriptor.Scoped<ITempDataDictionary, TempDataDictionary>());
            // This does caching so it should stay singleton
            services.TryAdd(ServiceDescriptor.Singleton<ITempDataProvider, SessionStateTempDataProvider>());

            return services;
        }

        /// <summary>
        /// Configures a set of <see cref="MvcOptions"/> for the application.
        /// </summary>
        /// <param name="services">The services available in the application.</param>
        /// <param name="setupAction">The <see cref="MvcOptions"/> which need to be configured.</param>
        public static void ConfigureMvc(
            [NotNull] this IServiceCollection services,
            [NotNull] Action<MvcOptions> setupAction)
        {
            services.Configure(setupAction);
        }

        /// <summary>
        /// Register the specified <paramref name="controllerTypes"/> as services and as a source for controller
        /// discovery.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="controllerTypes">A sequence of controller <see cref="Type"/>s to register in the
        /// <paramref name="services"/> and used for controller discovery.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection WithControllersAsServices(
           [NotNull] this IServiceCollection services,
           [NotNull] IEnumerable<Type> controllerTypes)
        {
            var controllerTypeProvider = new FixedSetControllerTypeProvider();
            foreach (var type in controllerTypes)
            {
                services.TryAdd(ServiceDescriptor.Transient(type, type));
                controllerTypeProvider.ControllerTypes.Add(type.GetTypeInfo());
            }

            services.Replace(ServiceDescriptor.Transient<IControllerActivator, ServiceBasedControllerActivator>());
            services.Replace(ServiceDescriptor.Instance<IControllerTypeProvider>(controllerTypeProvider));

            return services;
        }

        /// <summary>
        /// Registers controller types from the specified <paramref name="assemblies"/> as services and as a source
        /// for controller discovery.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="controllerAssemblies">Assemblies to scan.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection WithControllersAsServices(
            [NotNull] this IServiceCollection services,
            [NotNull] IEnumerable<Assembly> controllerAssemblies)
        {
            var assemblyProvider = new FixedSetAssemblyProvider();
            foreach (var assembly in controllerAssemblies)
            {
                assemblyProvider.CandidateAssemblies.Add(assembly);
            }

            var controllerTypeProvider = new DefaultControllerTypeProvider(assemblyProvider);
            var controllerTypes = controllerTypeProvider.ControllerTypes;

            return WithControllersAsServices(services, controllerTypes.Select(type => type.AsType()));
        }

        private static void ConfigureDefaultServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddDataProtection();
            services.AddRouting();
            services.AddCors();
            services.AddAuthorization();
            services.AddWebEncoders();
            services.Configure<RouteOptions>(
                routeOptions => routeOptions.ConstraintMap.Add("exists", typeof(KnownRouteValueConstraint)));
        }
    }
}