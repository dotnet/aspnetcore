// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ActionConstraints;
using Microsoft.AspNet.Mvc.ApplicationModels;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Metadata;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection.Extensions;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.Framework.DependencyInjection
{
    public static class MvcCoreServiceCollectionExtensions
    {
        public static IMvcBuilder AddMvcCore([NotNull] this IServiceCollection services)
        {
            ConfigureDefaultServices(services);

            AddMvcCoreServices(services);

            return new MvcBuilder() { Services = services, };
        }

        public static IMvcBuilder AddMvcCore(
            [NotNull] this IServiceCollection services,
            [NotNull] Action<MvcOptions> setupAction)
        {
            ConfigureDefaultServices(services);

            AddMvcCoreServices(services);

            services.Configure(setupAction);

            return new MvcBuilder() { Services = services, };
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

        // To enable unit testing
        internal static void AddMvcCoreServices(IServiceCollection services)
        {
            //
            // Options
            //
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, MvcCoreMvcOptionsSetup>());
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<RouteOptions>, MvcCoreRouteOptionsSetup>());

            //
            // Action Discovery
            //
            // These are consumed only when creating action descriptors, then they can be de-allocated
            services.TryAddTransient<IAssemblyProvider, DefaultAssemblyProvider>();
            services.TryAddTransient<IControllerTypeProvider, DefaultControllerTypeProvider>();
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IApplicationModelProvider, DefaultApplicationModelProvider>());
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IActionDescriptorProvider, ControllerActionDescriptorProvider>());
            services.TryAddSingleton<IActionDescriptorsCollectionProvider, DefaultActionDescriptorsCollectionProvider>();
            
            //
            // Action Selection
            //
            services.TryAddSingleton<IActionSelector, DefaultActionSelector>();

            // Performs caching
            services.TryAddSingleton<IActionSelectorDecisionTreeProvider, ActionSelectorDecisionTreeProvider>();

            // Will be cached by the DefaultActionSelector
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IActionConstraintProvider, DefaultActionConstraintProvider>());

            //
            // Controller Factory
            //
            // This has a cache, so it needs to be a singleton
            services.TryAddSingleton<IControllerFactory, DefaultControllerFactory>();

            // Will be cached by the DefaultControllerFactory
            services.TryAddTransient<IControllerActivator, DefaultControllerActivator>();
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IControllerPropertyActivator, DefaultControllerPropertyActivator>());

            //
            // Action Invoker
            //
            // These two access per-request services
            services.TryAddTransient<IActionInvokerFactory, ActionInvokerFactory>();
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IActionInvokerProvider, ControllerActionInvokerProvider>());

            // These are stateless
            services.TryAddSingleton<IControllerActionArgumentBinder, DefaultControllerActionArgumentBinder>();
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IFilterProvider, DefaultFilterProvider>());

            //
            // ModelBinding, Validation and Formatting
            //
            // The DefaultModelMetadataProvider does significant caching and should be a singleton.
            services.TryAddSingleton<IModelMetadataProvider, DefaultModelMetadataProvider>();
            services.TryAdd(ServiceDescriptor.Transient<ICompositeMetadataDetailsProvider>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<MvcOptions>>().Options;
                return new DefaultCompositeMetadataDetailsProvider(options.ModelMetadataDetailsProviders);
            }));
            services.TryAdd(ServiceDescriptor.Singleton<IObjectModelValidator>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<MvcOptions>>().Options;
                var modelMetadataProvider = serviceProvider.GetRequiredService<IModelMetadataProvider>();
                return new DefaultObjectValidator(options.ValidationExcludeFilters, modelMetadataProvider);
            }));

            //
            // Random Infrastructure
            //
            services.TryAddSingleton<MvcMarkerService, MvcMarkerService>();
            services.TryAddSingleton<ITypeActivatorCache, DefaultTypeActivatorCache>();
            services.TryAddScoped(typeof(IScopedInstance<>), typeof(ScopedInstance<>));
            services.TryAddScoped<IUrlHelper, UrlHelper>();
        }

        private static void ConfigureDefaultServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddRouting();
            services.AddNotifier();
        }

        private class MvcBuilder : IMvcBuilder
        {
            public IServiceCollection Services { get; set; }
        }
    }
}
