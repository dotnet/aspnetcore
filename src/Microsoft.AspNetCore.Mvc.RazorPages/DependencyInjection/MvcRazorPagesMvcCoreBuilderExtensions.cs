// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages.Internal;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MvcRazorPagesMvcCoreBuilderExtensions
    {
        public static IMvcCoreBuilder AddRazorPages(this IMvcCoreBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddRazorViewEngine();

            AddFeatureProviders(builder);
            AddServices(builder.Services);

            return builder;
        }

        public static IMvcCoreBuilder AddRazorPages(
            this IMvcCoreBuilder builder,
            Action<RazorPagesOptions> setupAction)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            builder.AddRazorViewEngine();

            AddFeatureProviders(builder);
            AddServices(builder.Services);

            builder.Services.Configure(setupAction);

            return builder;
        }

        private static void AddFeatureProviders(IMvcCoreBuilder builder)
        {
            if (!builder.PartManager.FeatureProviders.OfType<CompiledPageFeatureProvider>().Any())
            {
                builder.PartManager.FeatureProviders.Add(new CompiledPageFeatureProvider());
            }
        }

        // Internal for testing.
        internal static void AddServices(IServiceCollection services)
        {
            // Options
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<RazorPagesOptions>, RazorPagesOptionsSetup>());
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<RazorViewEngineOptions>, RazorPagesRazorViewEngineOptionsSetup>());

            // Action description and invocation
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IActionDescriptorProvider, PageActionDescriptorProvider>());
            services.TryAddSingleton<IActionDescriptorChangeProvider, PageActionDescriptorChangeProvider>();
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IPageApplicationModelProvider, RazorProjectPageApplicationModelProvider>());
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IPageApplicationModelProvider, CompiledPageApplicationModelProvider>());

            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IActionInvokerProvider, PageActionInvokerProvider>());

            // Page and Page model creation and activation
            services.TryAddSingleton<IPageModelActivatorProvider, DefaultPageModelActivatorProvider>();
            services.TryAddSingleton<IPageModelFactoryProvider, DefaultPageModelFactoryProvider>();

            services.TryAddSingleton<IPageActivatorProvider, DefaultPageActivator>();
            services.TryAddSingleton<IPageFactoryProvider, DefaultPageFactory>();

            services.TryAddSingleton<IPageLoader, DefaultPageLoader>();
            services.TryAddSingleton<IPageHandlerMethodSelector, DefaultPageHandlerMethodSelector>();

            // Page model binding
            services.TryAddSingleton<PageArgumentBinder, DefaultPageArgumentBinder>();

            // Action executors
            services.TryAddSingleton<PageResultExecutor>();
            services.TryAddSingleton<RedirectToPageResultExecutor>();

            services.TryAddTransient<PageSaveTempDataPropertyFilter>();
        }
    }
}
