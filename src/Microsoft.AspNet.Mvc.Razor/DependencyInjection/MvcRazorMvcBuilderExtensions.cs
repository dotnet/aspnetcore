// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.AspNet.Mvc.Razor.Compilation;
using Microsoft.AspNet.Mvc.Razor.Directives;
using Microsoft.AspNet.Mvc.Razor.Precompilation;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.DependencyInjection.Extensions;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.Framework.DependencyInjection
{
    public static class MvcRazorMvcBuilderExtensions
    {
        private static readonly Type RazorFileInfoCollectionType = typeof(RazorFileInfoCollection);

        public static IMvcBuilder AddRazorViewEngine([NotNull] this IMvcBuilder builder)
        {
            builder.AddViews();
            AddRazorViewEngineServices(builder.Services);
            return builder;
        }

        public static IMvcBuilder AddRazorViewEngine(
            [NotNull] this IMvcBuilder builder,
            [NotNull] Action<RazorViewEngineOptions> setupAction)
        {
            builder.AddViews();
            AddRazorViewEngineServices(builder.Services);

            if (setupAction != null)
            {
                builder.Services.Configure(setupAction);
            }

            return builder;
        }

        public static IMvcBuilder AddPrecompiledRazorViews(
            [NotNull] this IMvcBuilder builder,
            [NotNull] params Assembly[] assemblies)
        {
            AddRazorViewEngine(builder);

            var razorFileInfos = GetFileInfoCollections(assemblies);
            builder.Services.TryAddEnumerable(ServiceDescriptor.Instance(razorFileInfos));

            return builder;
        }

        public static IServiceCollection AddPrecompiledRazorViews(
            [NotNull] this IServiceCollection collection,
            [NotNull] params Assembly[] assemblies)
        {
            var razorFileInfos = GetFileInfoCollections(assemblies);
            collection.TryAddEnumerable(ServiceDescriptor.Instance(razorFileInfos));

            return collection;
        }

        public static IMvcBuilder ConfigureRazorViewEngine(
            [NotNull] this IMvcBuilder builder,
            [NotNull] Action<RazorViewEngineOptions> setupAction)
        {
            builder.Services.Configure(setupAction);
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

            // Caches view locations that are valid for the lifetime of the application.
            services.TryAddSingleton<IViewLocationCache, DefaultViewLocationCache>();
            services.TryAdd(ServiceDescriptor.Singleton<IChunkTreeCache>(serviceProvider =>
            {
                var cachedFileProvider = serviceProvider.GetRequiredService<IOptions<RazorViewEngineOptions>>();
                return new DefaultChunkTreeCache(cachedFileProvider.Options.FileProvider);
            }));

            // The host is designed to be discarded after consumption and is very inexpensive to initialize.
            services.TryAddTransient<IMvcRazorHost, MvcRazorHost>();

            // Caches compilation artifacts across the lifetime of the application.
            services.TryAddSingleton<ICompilerCache, CompilerCache>();

            // This caches compilation related details that are valid across the lifetime of the application
            // and is required to be a singleton.
            services.TryAddSingleton<ICompilationService, RoslynCompilationService>();

            // Both the compiler cache and roslyn compilation service hold on the compilation related
            // caches. RazorCompilation service is just an adapter service, and it is transient to ensure
            // the IMvcRazorHost dependency does not maintain state.
            services.TryAddTransient<IRazorCompilationService, RazorCompilationService>();

            // The ViewStartProvider needs to be able to consume scoped instances of IRazorPageFactory
            services.TryAddScoped<IViewStartProvider, ViewStartProvider>();
            services.TryAddTransient<IRazorViewFactory, RazorViewFactory>();
            services.TryAddSingleton<IRazorPageActivator, RazorPageActivator>();

            // Virtual path view factory needs to stay scoped so views can get get scoped services.
            services.TryAddScoped<IRazorPageFactory, VirtualPathRazorPageFactory>();

            // Only want one ITagHelperActivator so it can cache Type activation information. Types won't conflict.
            services.TryAddSingleton<ITagHelperActivator, DefaultTagHelperActivator>();

            // Consumed by the Cache tag helper to cache results across the lifetime of the application.
            services.TryAddSingleton<IMemoryCache, MemoryCache>();
        }

        private static IEnumerable<RazorFileInfoCollection> GetFileInfoCollections(IEnumerable<Assembly> assemblies) =>
            assemblies
                .SelectMany(assembly => assembly.ExportedTypes)
                .Where(IsValidRazorFileInfoCollection)
                .Select(Activator.CreateInstance)
                .Cast<RazorFileInfoCollection>();

        internal static bool IsValidRazorFileInfoCollection(Type type) =>
            RazorFileInfoCollectionType.IsAssignableFrom(type) &&
            !type.GetTypeInfo().IsAbstract &&
            !type.GetTypeInfo().ContainsGenericParameters;
    }
}
