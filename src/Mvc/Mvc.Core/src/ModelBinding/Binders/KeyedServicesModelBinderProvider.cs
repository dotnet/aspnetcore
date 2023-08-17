// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// An <see cref="IModelBinderProvider"/> for binding from the <see cref="IKeyedServiceProvider"/>.
/// </summary>
public class KeyedServicesModelBinderProvider : IModelBinderProvider
{
    /// <inheritdoc />
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.BindingInfo.BindingSource != null &&
            context.BindingInfo.BindingSource.CanAcceptDataFrom(BindingSource.KeyedServices))
        {
            // IsRequired will be false for a Reference Type
            // without a default value in a oblivious nullability context
            // however, for services we should treat them as required
            var isRequired = context.Metadata.IsRequired ||
                    (context.Metadata.Identity.ParameterInfo?.HasDefaultValue != true &&
                        !context.Metadata.ModelType.IsValueType &&
                        context.Metadata.NullabilityState == NullabilityState.Unknown);

            var attribute = context.Metadata.Identity.ParameterInfo?.GetCustomAttribute<FromKeyedServicesAttribute>();

            return new KeyedServicesModelBinder
            {
                IsOptional = !isRequired,
                Key = attribute?.Key
            };
        }

        return null;
    }
}
