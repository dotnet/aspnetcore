// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Enables component parameters to be supplied from the TempData with <see cref="SupplyParameterFromTempDataAttribute"/>.
/// </summary>
public static class SupplyParameterFromTempDataServiceCollectionExtensions
{
    /// <summary>
    /// Enables component parameters to be supplied from the TempData with <see cref="SupplyParameterFromTempDataAttribute"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddSupplyValueFromTempDataProvider(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ICascadingValueSupplier, SupplyParameterFromTempDataValueProvider>());
        return services;
    }
}
