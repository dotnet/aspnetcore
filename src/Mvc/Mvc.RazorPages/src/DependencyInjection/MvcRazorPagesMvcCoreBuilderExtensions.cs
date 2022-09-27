// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Resources = Microsoft.AspNetCore.Mvc.RazorPages.Resources;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Static class that adds razor page functionality to <see cref="IMvcCoreBuilder"/>.
/// </summary>
public static class MvcRazorPagesMvcCoreBuilderExtensions
{
    /// <summary>
    /// Register services needed for Razor Pages.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
    /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
    public static IMvcCoreBuilder AddRazorPages(this IMvcCoreBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.AddRazorViewEngine();

        AddRazorPagesServices(builder.Services);

        return builder;
    }

    /// <summary>
    /// Register services needed for Razor Pages.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
    /// <param name="setupAction">The action to setup the <see cref="RazorPagesOptions"/>.</param>
    /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
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

        AddRazorPagesServices(builder.Services);

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
    internal static void AddRazorPagesServices(IServiceCollection services)
    {
        // Options
        services.TryAddEnumerable(
            ServiceDescriptor.Transient<IConfigureOptions<RazorViewEngineOptions>, RazorPagesRazorViewEngineOptionsSetup>());

        services.TryAddEnumerable(
            ServiceDescriptor.Transient<IConfigureOptions<RazorPagesOptions>, RazorPagesOptionsSetup>());

        // Routing
        services.TryAddEnumerable(ServiceDescriptor.Singleton<MatcherPolicy, DynamicPageEndpointMatcherPolicy>());
        services.TryAddSingleton<DynamicPageEndpointSelectorCache>();
        services.TryAddSingleton<PageActionEndpointDataSourceIdProvider>();

        // Action description and invocation
        var actionDescriptorProvider = services.FirstOrDefault(f =>
            f.ServiceType == typeof(IActionDescriptorProvider) &&
            f.ImplementationType == typeof(PageActionDescriptorProvider));

        if (actionDescriptorProvider is null)
        {
            // RuntimeCompilation registers an instance of PageActionDescriptorProvider (PageADP). CompiledPageADP and runtime compilation
            // cannot co-exist since CompiledPageADP will attempt to resolve action descriptors for lazily compiled views (such as for
            // ones from non-physical file providers). We'll instead avoid adding it if PageADP is already registered. Similarly,
            // AddRazorRuntimeCompilation will remove CompiledPageADP if it is registered.
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IActionDescriptorProvider, CompiledPageActionDescriptorProvider>());
        }
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPageRouteModelProvider, CompiledPageRouteModelProvider>());
        services.TryAddSingleton<PageActionEndpointDataSourceFactory>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<MatcherPolicy, DynamicPageEndpointMatcherPolicy>());

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPageApplicationModelProvider, DefaultPageApplicationModelProvider>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPageApplicationModelProvider, AutoValidateAntiforgeryPageApplicationModelProvider>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPageApplicationModelProvider, AuthorizationPageApplicationModelProvider>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPageApplicationModelProvider, TempDataFilterPageApplicationModelProvider>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPageApplicationModelProvider, ViewDataAttributePageApplicationModelProvider>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPageApplicationModelProvider, ResponseCacheFilterApplicationModelProvider>());

        services.TryAddSingleton<IPageApplicationModelPartsProvider, DefaultPageApplicationModelPartsProvider>();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IActionInvokerProvider, PageActionInvokerProvider>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IRequestDelegateFactory, PageRequestDelegateFactory>());
        services.TryAddSingleton<PageActionInvokerCache>();

        // Page and Page model creation and activation
        services.TryAddSingleton<IPageModelActivatorProvider, DefaultPageModelActivatorProvider>();
        services.TryAddSingleton<IPageModelFactoryProvider, DefaultPageModelFactoryProvider>();

        services.TryAddSingleton<IPageActivatorProvider, DefaultPageActivatorProvider>();
        services.TryAddSingleton<IPageFactoryProvider, DefaultPageFactoryProvider>();

#pragma warning disable CS0618 // Type or member is obsolete
        services.TryAddSingleton<IPageLoader>(s => s.GetRequiredService<PageLoader>());
#pragma warning restore CS0618 // Type or member is obsolete
        services.TryAddSingleton<PageLoader, DefaultPageLoader>();
        services.TryAddSingleton<IPageHandlerMethodSelector, DefaultPageHandlerMethodSelector>();

        // Action executors
        services.TryAddSingleton<PageResultExecutor>();

        services.TryAddTransient<PageSaveTempDataPropertyFilter>();
    }
}
