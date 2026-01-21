// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Enables component parameters to be supplied from the session with <see cref="SupplyParameterFromSessionAttribute"/>.
/// </summary>
public static class SupplyParameterFromSessionServiceCollectionExtensions
{
    /// <summary>
    /// Enables component parameters to be supplied from the session with <see cref="SupplyParameterFromSessionAttribute"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddSupplyValueFromSessionProvider(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ICascadingValueSupplier, SupplyParameterFromSessionValueProvider>());
        return services;
    }
}
