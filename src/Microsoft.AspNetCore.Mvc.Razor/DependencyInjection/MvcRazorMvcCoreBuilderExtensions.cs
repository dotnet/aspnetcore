// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Razor.Host;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;
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
            AddRazorViewEngineFeatureProviders(builder);
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

            AddRazorViewEngineFeatureProviders(builder);
            AddRazorViewEngineServices(builder.Services);

            if (setupAction != null)
            {
                builder.Services.Configure(setupAction);
            }

            return builder;
        }

        private static void AddRazorViewEngineFeatureProviders(IMvcCoreBuilder builder)
        {
            if (!builder.PartManager.FeatureProviders.OfType<TagHelperFeatureProvider>().Any())
            {
                builder.PartManager.FeatureProviders.Add(new TagHelperFeatureProvider());
            }

            if (!builder.PartManager.FeatureProviders.OfType<MetadataReferenceFeatureProvider>().Any())
            {
                builder.PartManager.FeatureProviders.Add(new MetadataReferenceFeatureProvider());
            }

            if (!builder.PartManager.FeatureProviders.OfType<ViewsFeatureProvider>().Any())
            {
                builder.PartManager.FeatureProviders.Add(new ViewsFeatureProvider());
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
        /// <see cref="ITagHelper.ProcessAsync(TagHelperContext, TagHelperOutput)"/> method is called.
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
            services.TryAddSingleton<CSharpCompiler>();
            services.TryAddSingleton<RazorReferenceManager>();
            // This caches compilation related details that are valid across the lifetime of the application.
            services.TryAddSingleton<ICompilationService, DefaultRoslynCompilationService>();

            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<MvcViewOptions>, MvcRazorMvcViewOptionsSetup>());

            // DependencyContextRazorViewEngineOptionsSetup needs to run after RazorViewEngineOptionsSetup.
            // The ordering of the following two lines is important to ensure this behavior.
#pragma warning disable 0618
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<RazorViewEngineOptions>, RazorViewEngineOptionsSetup>());
#pragma warning restore 0618
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<
                    IConfigureOptions<RazorViewEngineOptions>,
                    DependencyContextRazorViewEngineOptionsSetup>());

            services.TryAddSingleton<
                IRazorViewEngineFileProviderAccessor,
                DefaultRazorViewEngineFileProviderAccessor>();

            services.TryAddSingleton<IRazorViewEngine, RazorViewEngine>();

            services.TryAddSingleton<ITagHelperTypeResolver, TagHelperTypeResolver>();
            services.TryAddSingleton<ITagHelperDescriptorFactory>(s => new TagHelperDescriptorFactory(designTime: false));
            services.TryAddSingleton<TagHelperDescriptorResolver>();
            services.TryAddSingleton<ViewComponentTagHelperDescriptorResolver>();
            services.TryAddSingleton<ITagHelperDescriptorResolver, CompositeTagHelperDescriptorResolver>();

            // Caches compilation artifacts across the lifetime of the application.
            services.TryAddSingleton<ICompilerCacheProvider, DefaultCompilerCacheProvider>();

            // In the default scenario the following services are singleton by virtue of being initialized as part of
            // creating the singleton RazorViewEngine instance.
            services.TryAddTransient<IRazorPageFactoryProvider, DefaultRazorPageFactoryProvider>();
            services.TryAddTransient<IRazorCompilationService, RazorCompilationService>();

            services.TryAddSingleton<RazorProject>(s =>
            {
                return new DefaultRazorProject(s.GetRequiredService<IRazorViewEngineFileProviderAccessor>().FileProvider);
            });

            services.TryAddSingleton<RazorEngine>(s =>
            {
                return RazorEngine.Create(b =>
                {
                    InjectDirective.Register(b);
                    ModelDirective.Register(b);
                    
                    b.Features.Add(new ModelExpressionPass());
                    b.Features.Add(new ViewComponentTagHelperPass());
                    b.Features.Add(new MvcViewDocumentClassifierPass());
                    b.Features.Add(new DefaultInstrumentationPass());

                    b.Features.Add(new Microsoft.CodeAnalysis.Razor.DefaultTagHelperFeature());

                    var referenceManager = s.GetRequiredService<RazorReferenceManager>();
                    b.Features.Add(new Microsoft.CodeAnalysis.Razor.DefaultMetadataReferenceFeature()
                    {
                        References = referenceManager.CompilationReferences.ToArray(),
                    });
                });
            });

            // This caches Razor page activation details that are valid for the lifetime of the application.
            services.TryAddSingleton<IRazorPageActivator, RazorPageActivator>();

            // Only want one ITagHelperActivator so it can cache Type activation information. Types won't conflict.
            services.TryAddSingleton<ITagHelperActivator, DefaultTagHelperActivator>();
            services.TryAddSingleton<ITagHelperFactory, DefaultTagHelperFactory>();

            // Consumed by the Cache tag helper to cache results across the lifetime of the application.
            services.TryAddSingleton<IMemoryCache, MemoryCache>();
        }
    }
}
