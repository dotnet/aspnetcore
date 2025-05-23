// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding <see cref="CircuitActivitySource"/> to the service collection.
/// </summary>
internal static class CircuitActivitySourceServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="CircuitActivitySource"/> to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddCircuitActivitySource(this IServiceCollection services)
    {
        services.TryAddSingleton<CircuitActivitySource>();
        return services;
    }
}