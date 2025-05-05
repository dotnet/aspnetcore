// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Enables component parameters to be supplied from <see cref="PersistentComponentState"/> with <see cref="SupplyParameterFromPersistentComponentStateAttribute"/>.
/// </summary>
public static class SupplyParameterFromPersistentComponentStateProviderServiceCollectionExtensions
{
    /// <summary>
    /// Enables component parameters to be supplied from <see cref="PersistentComponentState"/> with <see cref="SupplyParameterFromPersistentComponentStateAttribute"/>..
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddSupplyValueFromPersistentComponentStateProvider(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ICascadingValueSupplier, SupplyParameterFromPersistentComponentStateValueProvider>());
        return services;
    }
}
