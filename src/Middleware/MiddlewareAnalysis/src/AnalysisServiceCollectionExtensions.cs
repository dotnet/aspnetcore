// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.MiddlewareAnalysis;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up diagnostic services in an <see cref="IServiceCollection" />.
/// </summary>
public static class AnalysisServiceCollectionExtensions
{
    /// <summary>
    /// Adds diagnostic services to the specified <see cref="IServiceCollection" /> by logging to
    /// a <see cref="System.Diagnostics.DiagnosticSource"/> when middleware starts, finishes and throws.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddMiddlewareAnalysis(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Prevent registering the same implementation of IStartupFilter (AnalysisStartupFilter) multiple times.
        // But allow multiple registrations of different implementation types.
        services.TryAddEnumerable(ServiceDescriptor.Transient<IStartupFilter, AnalysisStartupFilter>());
        return services;
    }
}
