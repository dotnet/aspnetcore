// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.Components.Infrastructure;

/// <summary>
/// Enables component parameters to be supplied from <see cref="PersistentComponentState"/> with <see cref="PersistentStateAttribute"/>.
/// </summary>
public static class PersistentStateProviderServiceCollectionExtensions
{
    /// <summary>
    /// Enables component parameters to be supplied from <see cref="PersistentComponentState"/> with <see cref="PersistentStateAttribute"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddSupplyValueFromPersistentComponentStateProvider(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ICascadingValueSupplier, PersistentStateValueProvider>());
        return services;
    }
}
