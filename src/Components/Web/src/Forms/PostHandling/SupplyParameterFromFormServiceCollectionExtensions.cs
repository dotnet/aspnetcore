// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Binding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.Components;

public static class SupplyParameterFromFormServiceCollectionExtensions
{
    public static IServiceCollection AddSupplyValueFromFormProvider(this IServiceCollection serviceCollection)
    {
        serviceCollection.TryAddEnumerable(ServiceDescriptor.Scoped<ICascadingValueSupplier, SupplyParameterFromFormValueProvider>(services =>
        {
            return new SupplyParameterFromFormValueProvider(
                services.GetRequiredService<IFormValueSupplier>(),
                services.GetRequiredService<NavigationManager>(),
                null, "");
        }));

        return serviceCollection;
    }
}
