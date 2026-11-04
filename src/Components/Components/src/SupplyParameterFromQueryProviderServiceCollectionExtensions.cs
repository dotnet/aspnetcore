// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Enables component parameters to be supplied from the query string with <see cref="SupplyParameterFromQueryAttribute"/>.
/// </summary>
public static class SupplyParameterFromQueryProviderServiceCollectionExtensions
{
    /// <summary>
    /// Enables component parameters to be supplied from the query string with <see cref="SupplyParameterFromQueryAttribute"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddSupplyValueFromQueryProvider(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ICascadingValueSupplier, SupplyParameterFromQueryValueProvider>());
        return services;
    }
}
