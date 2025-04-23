// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.Components.Infrastructure;

/// <summary>
/// Infrastructure APIs for registering diagnostic metrics.
/// </summary>
public static class RenderingMetricsServiceCollectionExtensions
{
    /// <summary>
    /// Registers component rendering metrics
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddRenderingMetrics(
        IServiceCollection services)
    {
        if (RenderingMetrics.IsMetricsSupported)
        {
            services.AddMetrics();
            services.TryAddSingleton<RenderingMetrics>();
        }

        return services;
    }
}
