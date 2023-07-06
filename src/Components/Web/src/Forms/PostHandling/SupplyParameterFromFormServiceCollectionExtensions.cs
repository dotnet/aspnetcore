// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Binding;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components;

public static class SupplyParameterFromFormServiceCollectionExtensions
{
    public static IServiceCollection AddSupplyValueFromFormProvider(this IServiceCollection serviceCollection)
    {
        return serviceCollection.AddScoped<ICascadingValueSupplier, SupplyParameterFromFormValueProvider>(services =>
        {
            var result = new SupplyParameterFromFormValueProvider();
            result.UpdateBindingInformation(
                services.GetRequiredService<IFormValueSupplier>(),
                services.GetRequiredService<NavigationManager>(),
                null, "");
            return result;
        });
    }
}
