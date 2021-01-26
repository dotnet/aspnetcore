// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Routing;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Static class that adds razor runtime compilation extension methods.
    /// </summary>
    public static class RazorRuntimeCompilationMvcCoreBuilderExtensions
    {
        /// <summary>
        /// Configures <see cref="IMvcCoreBuilder" /> to support runtime compilation of Razor views and Razor Pages.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcCoreBuilder" />.</param>
        /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
        public static IMvcCoreBuilder AddRazorRuntimeCompilation(this IMvcCoreBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            AddServices(builder.Services);
            return builder;
        }

        /// <summary>
        /// Configures <see cref="IMvcCoreBuilder" /> to support runtime compilation of Razor views and Razor Pages.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcCoreBuilder" />.</param>
        /// <param name="setupAction">An action to configure the <see cref="MvcRazorRuntimeCompilationOptions"/>.</param>
        /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
        public static IMvcCoreBuilder AddRazorRuntimeCompilation(this IMvcCoreBuilder builder, Action<MvcRazorRuntimeCompilationOptions> setupAction)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            AddServices(builder.Services);
            builder.Services.Configure(setupAction);
            return builder;
        }

        // Internal for testing.
        internal static void AddServices(IServiceCollection services)
        {
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<MvcRazorRuntimeCompilationOptions>, MvcRazorRuntimeCompilationOptionsSetup>());

            var compilerProvider = services.FirstOrDefault(f =>
                f.ServiceType == typeof(IViewCompilerProvider) &&
                f.ImplementationType?.Assembly == typeof(IViewCompilerProvider).Assembly &&
                f.ImplementationType.FullName == "Microsoft.AspNetCore.Mvc.Razor.Compilation.DefaultViewCompilerProvider");

            if (compilerProvider != null)
            {
                // Replace the default implementation of IViewCompilerProvider
                services.Remove(compilerProvider);
            }

            services.TryAddSingleton<IViewCompilerProvider, RuntimeViewCompilerProvider>();

            var actionDescriptorProvider = services.FirstOrDefault(f =>
                f.ServiceType == typeof(IActionDescriptorProvider) &&
                f.ImplementationType == typeof(CompiledPageActionDescriptorProvider));

            if (actionDescriptorProvider != null)
            {
                services.Remove(actionDescriptorProvider);

                // Add PageActionDescriptorProvider and the matcher policy that supports runtime compilation.
                // We only want to add support for this if we know AddRazorPages was called. In the absence of this, several services registered by Razor Pages
                // will be absent. We'll use the presence of the CompiledPageActionDescriptorProvider service as a poor way to test this.
                services.TryAddEnumerable(
                    ServiceDescriptor.Singleton<IActionDescriptorProvider, PageActionDescriptorProvider>());

                services.TryAddEnumerable(ServiceDescriptor.Singleton<MatcherPolicy, PageLoaderMatcherPolicy>());
            }

            services.TryAddSingleton<RuntimeCompilationFileProvider>();
            services.TryAddSingleton<RazorReferenceManager>();
            services.TryAddSingleton<CSharpCompiler>();

            services.TryAddSingleton<RazorProjectFileSystem, FileProviderRazorProjectFileSystem>();
            services.TryAddSingleton(s =>
            {
                var fileSystem = s.GetRequiredService<RazorProjectFileSystem>();
                var csharpCompiler = s.GetRequiredService<CSharpCompiler>();
                var projectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, fileSystem, builder =>
                {
                    RazorExtensions.Register(builder);

                    // Roslyn + TagHelpers infrastructure
                    var referenceManager = s.GetRequiredService<RazorReferenceManager>();
                    builder.Features.Add(new LazyMetadataReferenceFeature(referenceManager));
                    builder.Features.Add(new CompilationTagHelperFeature());

                    // TagHelperDescriptorProviders (actually do tag helper discovery)
                    builder.Features.Add(new DefaultTagHelperDescriptorProvider());
                    builder.Features.Add(new ViewComponentTagHelperDescriptorProvider());
                    builder.SetCSharpLanguageVersion(csharpCompiler.ParseOptions.LanguageVersion);
                });

                return projectEngine;
            });

            //
            // Razor Pages
            //
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IPageRouteModelProvider, RazorProjectPageRouteModelProvider>());

            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IActionDescriptorChangeProvider, PageActionDescriptorChangeProvider>());
        }
    }
}
