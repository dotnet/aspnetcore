// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Actions;
using Microsoft.AspNet.Mvc.Formatters;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.ViewComponents;
using Microsoft.Framework.DependencyInjection.Extensions;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.Framework.DependencyInjection
{
    public static class MvcViewFeaturesMvcCoreBuilderExtensions
    {
        public static IMvcCoreBuilder AddViews([NotNull] this IMvcCoreBuilder builder)
        {
            builder.AddDataAnnotations();
            AddViewServices(builder.Services);
            return builder;
        }

        public static IMvcCoreBuilder AddViews(
            [NotNull] this IMvcCoreBuilder builder,
            [NotNull] Action<MvcViewOptions> setupAction)
        {
            builder.AddDataAnnotations();
            AddViewServices(builder.Services);

            if (setupAction != null)
            {
                builder.Services.Configure(setupAction);
            }

            return builder;
        }

        public static IMvcCoreBuilder ConfigureViews(
            [NotNull] this IMvcCoreBuilder builder,
            [NotNull] Action<MvcViewOptions> setupAction)
        {
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

            // Support for activating ViewDataDictionary
            services.TryAddEnumerable(
                ServiceDescriptor
                    .Transient<IControllerPropertyActivator, ViewDataDictionaryControllerPropertyActivator>());

            //
            // HTML Helper
            //
            services.TryAddTransient<IHtmlHelper, HtmlHelper>();
            services.TryAddTransient(typeof(IHtmlHelper<>), typeof(HtmlHelper<>));

            // DefaultHtmlGenerator is pretty much stateless but depends on IUrlHelper, which is scoped.
            // Therefore it too is scoped.
            services.TryAddScoped<IHtmlGenerator, DefaultHtmlGenerator>();

            //
            // JSON Helper
            //
            services.TryAddSingleton<IJsonHelper, JsonHelper>();
            services.TryAdd(ServiceDescriptor.Singleton<JsonOutputFormatter>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<MvcJsonOptions>>().Value;
                return new JsonOutputFormatter(options.SerializerSettings);
            }));

            //
            // View Components
            //
            // These do caching so they should stay singleton
            services.TryAddSingleton<IViewComponentSelector, DefaultViewComponentSelector>();
            services.TryAddSingleton<IViewComponentActivator, DefaultViewComponentActivator>();
            services.TryAddSingleton<
                IViewComponentDescriptorCollectionProvider,
                DefaultViewComponentDescriptorCollectionProvider>();

            services.TryAddTransient<IViewComponentDescriptorProvider, DefaultViewComponentDescriptorProvider>();
            services.TryAddSingleton<IViewComponentInvokerFactory, DefaultViewComponentInvokerFactory>();
            services.TryAddTransient<IViewComponentHelper, DefaultViewComponentHelper>();

            //
            // Temp Data
            //
            // Holds per-request data so it should be scoped
            services.TryAddScoped<ITempDataDictionary, TempDataDictionary>();
            services.TryAddScoped<SaveTempDataFilter>();

            // This does caching so it should stay singleton
            services.TryAddSingleton<ITempDataProvider, SessionStateTempDataProvider>();
        }
    }
}
