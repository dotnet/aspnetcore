// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Razor.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MvcRazorMvcCoreBuilderExtensions
    {
        public static IMvcCoreBuilder AddRazorViewEngine(this IMvcCoreBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddViews();
            AddRazorViewEngineFeatureProviders(builder.PartManager);
            AddRazorViewEngineServices(builder.Services);
            return builder;
        }

        public static IMvcCoreBuilder AddRazorViewEngine(
            this IMvcCoreBuilder builder,
            Action<RazorViewEngineOptions> setupAction)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            builder.AddViews();

            AddRazorViewEngineFeatureProviders(builder.PartManager);
            AddRazorViewEngineServices(builder.Services);

            builder.Services.Configure(setupAction);

            return builder;
        }

        internal static void AddRazorViewEngineFeatureProviders(ApplicationPartManager partManager)
        {
            if (!partManager.FeatureProviders.OfType<TagHelperFeatureProvider>().Any())
            {
                partManager.FeatureProviders.Add(new TagHelperFeatureProvider());
            }

            // ViewFeature items have precedence semantics - when two views have the same path \ identifier,
            // the one that appears earlier in the list wins. Therefore the ordering of
            // RazorCompiledItemFeatureProvider and ViewsFeatureProvider is pertinent - any view compiled
            // using the Sdk will be preferred to views compiled using MvcPrecompilation.
            if (!partManager.FeatureProviders.OfType<RazorCompiledItemFeatureProvider>().Any())
            {
                partManager.FeatureProviders.Add(new RazorCompiledItemFeatureProvider());
            }
        }

        /// <summary>
        /// Registers discovered tag helpers as services and changes the existing <see cref="ITagHelperActivator"/>
        /// for an <see cref="ServiceBasedTagHelperActivator"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcCoreBuilder"/> instance this method extends.</param>
        /// <returns>The <see cref="IMvcCoreBuilder"/> instance this method extends.</returns>
        public static IMvcCoreBuilder AddTagHelpersAsServices(this IMvcCoreBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            TagHelpersAsServices.AddTagHelpersAsServices(builder.PartManager, builder.Services);
            return builder;
        }

        /// <summary>
        /// Adds an initialization callback for a given <typeparamref name="TTagHelper"/>.
        /// </summary>
        /// <remarks>
        /// The callback will be invoked on any <typeparamref name="TTagHelper"/> instance before the
        /// <see cref="ITagHelperComponent.ProcessAsync(TagHelperContext, TagHelperOutput)"/> method is called.
        /// </remarks>
        /// <typeparam name="TTagHelper">The type of <see cref="ITagHelper"/> being initialized.</typeparam>
        /// <param name="builder">The <see cref="IMvcCoreBuilder"/> instance this method extends.</param>
        /// <param name="initialize">An action to initialize the <typeparamref name="TTagHelper"/>.</param>
        /// <returns>The <see cref="IMvcCoreBuilder"/> instance this method extends.</returns>
        public static IMvcCoreBuilder InitializeTagHelper<TTagHelper>(
            this IMvcCoreBuilder builder,
            Action<TTagHelper, ViewContext> initialize)
            where TTagHelper : ITagHelper
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (initialize == null)
            {
                throw new ArgumentNullException(nameof(initialize));
            }

            var initializer = new TagHelperInitializer<TTagHelper>(initialize);

            builder.Services.AddSingleton(typeof(ITagHelperInitializer<TTagHelper>), initializer);

            return builder;
        }

        // Internal for testing.
        internal static void AddRazorViewEngineServices(IServiceCollection services)
        {
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<MvcViewOptions>, MvcRazorMvcViewOptionsSetup>());

            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<RazorViewEngineOptions>, RazorViewEngineOptionsSetup>());

            services.TryAddSingleton<IRazorViewEngine, RazorViewEngine>();
            services.TryAddSingleton<IViewCompilerProvider, DefaultViewCompilerProvider>();

            // In the default scenario the following services are singleton by virtue of being initialized as part of
            // creating the singleton RazorViewEngine instance.
            services.TryAddTransient<IRazorPageFactoryProvider, DefaultRazorPageFactoryProvider>();

            // This caches Razor page activation details that are valid for the lifetime of the application.
            services.TryAddSingleton<IRazorPageActivator, RazorPageActivator>();

            // Only want one ITagHelperActivator and ITagHelperComponentPropertyActivator so it can cache Type activation information. Types won't conflict.
            services.TryAddSingleton<ITagHelperActivator, DefaultTagHelperActivator>();
            services.TryAddSingleton<ITagHelperComponentPropertyActivator, TagHelperComponentPropertyActivator>();

            services.TryAddSingleton<ITagHelperFactory, DefaultTagHelperFactory>();

            // TagHelperComponents manager
            services.TryAddScoped<ITagHelperComponentManager, TagHelperComponentManager>();

            // Infrastructure for MVC TagHelpers
            services.TryAddSingleton<IMemoryCache, MemoryCache>();
            services.TryAddSingleton<TagHelperMemoryCacheProvider>();
            services.TryAddSingleton<IFileVersionProvider, DefaultFileVersionProvider>();
        }
    }
}
