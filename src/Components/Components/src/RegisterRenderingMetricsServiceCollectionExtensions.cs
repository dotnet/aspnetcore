// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.Components.Infrastructure;

/// <summary>
/// Infrastructure APIs for registering diagnostic metrics.
/// </summary>
public static class ComponentsMetricsServiceCollectionExtensions
{
    /// <summary>
    /// Registers component rendering metrics
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddComponentsMetrics(
        IServiceCollection services)
    {
        // do not register IConfigureOptions<StartupValidatorOptions> multiple times
        if (!IsMeterFactoryRegistered(services))
        {
            services.AddMetrics();
        }
        services.TryAddSingleton<ComponentsMetrics>();

        return services;
    }

    /// <summary>
    /// Registers component rendering traces
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddComponentsTracing(
        IServiceCollection services)
    {
        services.TryAddScoped<ComponentsActivitySource>();

        return services;
    }

    private static bool IsMeterFactoryRegistered(IServiceCollection services)
    {
        foreach (var service in services)
        {
            if (service.ServiceType == typeof(IMeterFactory))
            {
                return true;
            }
        }
        return false;
    }
}
