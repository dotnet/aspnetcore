// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for the request localization middleware.
/// </summary>
public static class RequestLocalizationServiceCollectionExtensions
{
    /// <summary>
    /// Adds services and options for the request localization middleware.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <param name="configureOptions">A delegate to configure the <see cref="RequestLocalizationOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddRequestLocalization(this IServiceCollection services, Action<RequestLocalizationOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        return services.Configure(configureOptions);
    }

    /// <summary>
    /// Adds services and options for the request localization middleware.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <param name="configureOptions">A delegate to configure the <see cref="RequestLocalizationOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddRequestLocalization<TService>(this IServiceCollection services, Action<RequestLocalizationOptions, TService> configureOptions) where TService : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddOptions<RequestLocalizationOptions>().Configure(configureOptions);
        return services;
    }
}
