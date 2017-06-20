// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages.Internal;
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

            AddServices(builder.Services);

            builder.Services.Configure(setupAction);

            return builder;
        }

        /// <summary>
        /// Configures Razor Pages to use the specified <paramref name="rootDirectory"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
        /// <param name="rootDirectory">The application relative path to use as the root directory.</param>
        /// <returns></returns>
        public static IMvcCoreBuilder WithRazorPagesRoot(this IMvcCoreBuilder builder, string rootDirectory)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (string.IsNullOrEmpty(rootDirectory))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(rootDirectory));
            }

            builder.Services.Configure<RazorPagesOptions>(options => options.RootDirectory = rootDirectory);
            return builder;
        }

        // Internal for testing.
        internal static void AddServices(IServiceCollection services)
        {
            // Options
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<RazorViewEngineOptions>, RazorPagesRazorViewEngineOptionsSetup>());

            // Action description and invocation
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IActionDescriptorProvider, PageActionDescriptorProvider>());
            services.TryAddSingleton<IActionDescriptorChangeProvider, PageActionDescriptorChangeProvider>();
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IPageRouteModelProvider, RazorProjectPageRouteModelProvider>());
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IPageRouteModelProvider, CompiledPageRouteModelProvider>());

            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IPageApplicationModelProvider, DefaultPageApplicationModelProvider>());
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IPageApplicationModelProvider, AutoValidateAntiforgeryPageApplicationModelProvider>());
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IPageApplicationModelProvider, AuthorizationPageApplicationModelProvider>());
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IPageApplicationModelProvider, TempDataFilterPageApplicationModelProvider>());

            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IActionInvokerProvider, PageActionInvokerProvider>());

            // Page and Page model creation and activation
            services.TryAddSingleton<IPageModelActivatorProvider, DefaultPageModelActivatorProvider>();
            services.TryAddSingleton<IPageModelFactoryProvider, DefaultPageModelFactoryProvider>();

            services.TryAddSingleton<IPageActivatorProvider, DefaultPageActivatorProvider>();
            services.TryAddSingleton<IPageFactoryProvider, DefaultPageFactoryProvider>();

            services.TryAddSingleton<IPageLoader, DefaultPageLoader>();
            services.TryAddSingleton<IPageHandlerMethodSelector, DefaultPageHandlerMethodSelector>();

            // Page model binding
            services.TryAddSingleton<PageArgumentBinder, DefaultPageArgumentBinder>();

            // Action executors
            services.TryAddSingleton<PageResultExecutor>();

            services.TryAddTransient<PageSaveTempDataPropertyFilter>();
        }
    }
}
