// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Enables component parameters to be supplied from the TempData with <see cref="SupplyParameterFromTempDataAttribute"/>.
/// </summary>
public static class SupplyParameterFromTempDataServiceCollectionExtensions
{
    /// <summary>
    /// Enables component parameters to be supplied from the TempData with <see cref="SupplyParameterFromTempDataAttribute"/>.
    /// </summary>
    public static IServiceCollection AddSupplyValueFromTempDataProvider(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ICascadingValueSupplier, SupplyParameterFromTempDataValueProvider>());
        return services;
    }
}
