// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.Components.QuickGrid;

/// <summary>
/// Service registration helpers for QuickGrid localization.
/// </summary>
public static class QuickGridServiceCollectionExtensions
{
    /// <summary>
    /// Adds the default QuickGrid localization services: the default interceptor and internal localizer.
    /// </summary>
    public static IServiceCollection AddQuickGridLocalization(this IServiceCollection services)
    {
        // Register the public localizer (apps may override)
        services.TryAddSingleton<QuickGridLocalizer, QuickGridLocalizer>();

        // Register the interceptor using the built-in resources and any app-provided QuickGridLocalizer
        services.TryAddSingleton<IQuickGridLocalizationInterceptor>(sp =>
        {
            var rm = new ResourceManager(typeof(QuickGridResources));
            var localizer = sp.GetService<QuickGridLocalizer>();
            return new DefaultQuickGridLocalizationInterceptor(rm, localizer);
        });

        // Internal entry point used by components
        services.TryAddSingleton<InternalQuickGridLocalizer>(sp => new InternalQuickGridLocalizer(sp.GetRequiredService<IQuickGridLocalizationInterceptor>()));

        return services;
    }

    /// <summary>
    /// Registers a custom `IQuickGridLocalizationInterceptor` implementation.
    /// </summary>
    /// <summary>
    /// Registers a custom <see cref="IQuickGridLocalizationInterceptor"/> implementation using a factory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">Factory that creates the interceptor.</param>
    public static IServiceCollection AddQuickGridLocalizationInterceptor(this IServiceCollection services, Func<IServiceProvider, IQuickGridLocalizationInterceptor> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        services.TryAddSingleton<IQuickGridLocalizationInterceptor>(factory);
        return services;
    }

    /// <summary>
    /// Registers a custom `QuickGridLocalizer` implementation.
    /// </summary>
    /// <summary>
    /// Registers a custom <see cref="QuickGridLocalizer"/> implementation using a factory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">Factory that creates the localizer.</param>
    public static IServiceCollection AddQuickGridLocalizer(this IServiceCollection services, Func<IServiceProvider, QuickGridLocalizer> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        services.TryAddSingleton<QuickGridLocalizer>(factory);
        return services;
    }
}
