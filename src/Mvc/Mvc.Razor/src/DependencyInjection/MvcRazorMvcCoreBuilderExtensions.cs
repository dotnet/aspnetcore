// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection.Metadata;
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

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Static class that adds RazorViewEngine methods to <see cref="IMvcCoreBuilder"/>.
/// </summary>
public static class MvcRazorMvcCoreBuilderExtensions
{
    /// <summary>
    /// Registers Razor view engine services.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
    /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
    public static IMvcCoreBuilder AddRazorViewEngine(this IMvcCoreBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddViews();
        AddRazorViewEngineFeatureProviders(builder.PartManager);
        AddRazorViewEngineServices(builder.Services);
        return builder;
    }

    /// <summary>
    /// Registers Razor view engine services.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
    /// <param name="setupAction">A setup action that configures the <see cref="RazorViewEngineOptions"/>.</param>
    /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
    public static IMvcCoreBuilder AddRazorViewEngine(
        this IMvcCoreBuilder builder,
        Action<RazorViewEngineOptions> setupAction)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(setupAction);

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
        ArgumentNullException.ThrowIfNull(builder);

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
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(initialize);

        var initializer = new TagHelperInitializer<TTagHelper>(initialize);

        builder.Services.AddSingleton(typeof(ITagHelperInitializer<TTagHelper>), initializer);

        return builder;
    }

    // Internal for testing.
    internal static void AddRazorViewEngineServices(IServiceCollection services)
    {
        if (MetadataUpdater.IsSupported)
        {
            services.TryAddSingleton<RazorHotReload>();
        }

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
