// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.Components.Forms.Mapping;

/// <summary>
/// Extension methods for configuring <see cref="SupplyParameterFromFormAttribute"/> within an <see cref="IServiceCollection"/>.
/// </summary>
public static class SupplyParameterFromFormServiceCollectionExtensions
{
    /// <summary>
    /// Adds support for <see cref="SupplyParameterFromFormAttribute"/> within the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="serviceCollection">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddSupplyValueFromFormProvider(this IServiceCollection serviceCollection)
    {
        serviceCollection.TryAddEnumerable(ServiceDescriptor.Scoped<ICascadingValueSupplier, SupplyParameterFromFormValueProvider>(services =>
        {
            return new SupplyParameterFromFormValueProvider(
                services.GetRequiredService<IFormValueMapper>(),
                mappingScopeName: "");
        }));

        return serviceCollection;
    }
}
