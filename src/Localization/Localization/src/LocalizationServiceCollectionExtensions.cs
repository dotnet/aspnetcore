// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up localization services in an <see cref="IServiceCollection" />.
/// </summary>
public static class LocalizationServiceCollectionExtensions
{
    /// <summary>
    /// Adds services required for application localization.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddLocalization(this IServiceCollection services)
    {
        ArgumentNullThrowHelper.ThrowIfNull(services);

        services.AddOptions();

        AddLocalizationServices(services);

        return services;
    }

    /// <summary>
    /// Adds services required for application localization.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="setupAction">
    /// An <see cref="Action{LocalizationOptions}"/> to configure the <see cref="LocalizationOptions"/>.
    /// </param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddLocalization(
        this IServiceCollection services,
        Action<LocalizationOptions> setupAction)
    {
        ArgumentNullThrowHelper.ThrowIfNull(services);
        ArgumentNullThrowHelper.ThrowIfNull(setupAction);

        services.AddOptions();
        
        AddLocalizationServices(services, setupAction);

        return services;
    }

    // To enable unit testing
    internal static void AddLocalizationServices(IServiceCollection services)
    {
        services.TryAddSingleton<IStringLocalizerFactory, ResourceManagerStringLocalizerFactory>();
        services.TryAddTransient(typeof(IStringLocalizer<>), typeof(StringLocalizer<>));
    }

    internal static void AddLocalizationServices(
        IServiceCollection services,
        Action<LocalizationOptions> setupAction)
    {
        AddLocalizationServices(services);
        services.Configure(setupAction);
    }
}
