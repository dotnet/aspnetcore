// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Linq;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures.RazorComponents;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MvcViewFeaturesMvcCoreBuilderExtensions
    {
        public static IMvcCoreBuilder AddViews(this IMvcCoreBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddDataAnnotations();
            AddViewComponentApplicationPartsProviders(builder.PartManager);
            AddViewServices(builder.Services);
            return builder;
        }

        /// <summary>
        /// Registers <see cref="CookieTempDataProvider"/> as the default <see cref="ITempDataProvider"/> in the
        /// <see cref="IServiceCollection"/>. Also registers the default view services.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
        /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
        public static IMvcCoreBuilder AddCookieTempDataProvider(this IMvcCoreBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            // Ensure the TempData basics are registered.
            AddViewServices(builder.Services);

            var descriptor = ServiceDescriptor.Singleton(typeof(ITempDataProvider), typeof(CookieTempDataProvider));
            builder.Services.Replace(descriptor);

            return builder;
        }

        internal static void AddViewComponentApplicationPartsProviders(ApplicationPartManager manager)
        {
            if (!manager.FeatureProviders.OfType<ViewComponentFeatureProvider>().Any())
            {
                manager.FeatureProviders.Add(new ViewComponentFeatureProvider());
            }
        }

        public static IMvcCoreBuilder AddViews(
            this IMvcCoreBuilder builder,
            Action<MvcViewOptions> setupAction)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            AddViews(builder);
            builder.Services.Configure(setupAction);

            return builder;
        }

        /// <summary>
        /// Registers <see cref="CookieTempDataProvider"/> as the default <see cref="ITempDataProvider"/> in the
        /// <see cref="IServiceCollection"/>. Also registers the default view services.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
        /// <param name="setupAction">
        /// An <see cref="Action{CookieTempDataProviderOptions}"/> to configure the provided
        /// <see cref="CookieTempDataProviderOptions"/>.
        /// </param>
        /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
        public static IMvcCoreBuilder AddCookieTempDataProvider(
            this IMvcCoreBuilder builder,
            Action<CookieTempDataProviderOptions> setupAction)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            AddCookieTempDataProvider(builder);
            builder.Services.Configure(setupAction);

            return builder;
        }

        public static IMvcCoreBuilder ConfigureViews(
            this IMvcCoreBuilder builder,
            Action<MvcViewOptions> setupAction)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            builder.Services.Configure(setupAction);
            return builder;
        }

        // Internal for testing.
        internal static void AddViewServices(IServiceCollection services)
        {
            services.AddDataProtection();
            services.AddAntiforgery();
            services.AddWebEncoders();

            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<MvcViewOptions>, MvcViewOptionsSetup>());
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, TempDataMvcOptionsSetup>());

            //
            // View Engine and related infrastructure
            //
            services.TryAddSingleton<ICompositeViewEngine, CompositeViewEngine>();
            services.TryAddSingleton<IActionResultExecutor<ViewResult>, ViewResultExecutor>();
            services.TryAddSingleton<IActionResultExecutor<PartialViewResult>, PartialViewResultExecutor>();

            // Support for activating ViewDataDictionary
            services.TryAddEnumerable(
                ServiceDescriptor
                    .Transient<IControllerPropertyActivator, ViewDataDictionaryControllerPropertyActivator>());

            //
            // HTML Helper
            //
            services.TryAddTransient<IHtmlHelper, HtmlHelper>();
            services.TryAddTransient(typeof(IHtmlHelper<>), typeof(HtmlHelper<>));
            services.TryAddSingleton<IHtmlGenerator, DefaultHtmlGenerator>();
            services.TryAddSingleton<ModelExpressionProvider>();
            // ModelExpressionProvider caches results. Ensure that it's re-used when the requested type is IModelExpressionProvider.
            services.TryAddSingleton<IModelExpressionProvider>(s => s.GetRequiredService<ModelExpressionProvider>());
            services.TryAddSingleton<ValidationHtmlAttributeProvider, DefaultValidationHtmlAttributeProvider>();

            services.TryAddSingleton<IJsonHelper, DefaultJsonHelper>();

            //
            // View Components
            //

            // These do caching so they should stay singleton
            services.TryAddSingleton<IViewComponentSelector, DefaultViewComponentSelector>();
            services.TryAddSingleton<IViewComponentFactory, DefaultViewComponentFactory>();
            services.TryAddSingleton<IViewComponentActivator, DefaultViewComponentActivator>();
            services.TryAddSingleton<
                IViewComponentDescriptorCollectionProvider,
                DefaultViewComponentDescriptorCollectionProvider>();
            services.TryAddSingleton<IActionResultExecutor<ViewComponentResult>, ViewComponentResultExecutor>();

            services.TryAddSingleton<ViewComponentInvokerCache>();
            services.TryAddTransient<IViewComponentDescriptorProvider, DefaultViewComponentDescriptorProvider>();
            services.TryAddSingleton<IViewComponentInvokerFactory, DefaultViewComponentInvokerFactory>();
            services.TryAddTransient<IViewComponentHelper, DefaultViewComponentHelper>();

            //
            // Temp Data
            //
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IApplicationModelProvider, TempDataApplicationModelProvider>());
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IApplicationModelProvider, ViewDataAttributeApplicationModelProvider>());
            services.TryAddSingleton<SaveTempDataFilter>();

            //
            // Component prerendering
            //
            services.TryAddSingleton<IComponentPrerenderer, MvcRazorComponentPrerenderer>();
            services.TryAddScoped<IUriHelper, HttpUriHelper>();
            services.TryAddScoped<IJSRuntime, UnsupportedJavaScriptRuntime>();

            services.TryAddTransient<ControllerSaveTempDataPropertyFilter>();

            // This does caching so it should stay singleton
            services.TryAddSingleton<ITempDataProvider, CookieTempDataProvider>();
            services.TryAddSingleton<TempDataSerializer, DefaultTempDataSerializer>();

            //
            // Antiforgery
            //
            services.TryAddSingleton<ValidateAntiforgeryTokenAuthorizationFilter>();
            services.TryAddSingleton<AutoValidateAntiforgeryTokenAuthorizationFilter>();

            // These are stateless so their lifetime isn't really important.
            services.TryAddSingleton<ITempDataDictionaryFactory, TempDataDictionaryFactory>();
            services.TryAddSingleton(ArrayPool<ViewBufferValue>.Shared);
            services.TryAddScoped<IViewBufferScope, MemoryPoolViewBufferScope>();
        }
    }
}
