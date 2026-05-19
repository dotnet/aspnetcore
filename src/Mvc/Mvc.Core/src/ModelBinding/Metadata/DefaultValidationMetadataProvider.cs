// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

/// <summary>
/// A default implementation of <see cref="IValidationMetadataProvider"/>.
/// </summary>
internal sealed class DefaultValidationMetadataProvider : IValidationMetadataProvider
{
    /// <inheritdoc />
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var attribute in context.Attributes)
        {
            if (attribute is IModelValidator || attribute is IClientModelValidator)
            {
                // If another provider has already added this attribute, do not repeat it.
                // This will prevent attributes like RemoteAttribute (which implement ValidationAttribute and
                // IClientModelValidator) to be added to the ValidationMetadata twice.
                // This is to ensure we do not end up with duplication validation rules on the client side.
                if (!context.ValidationMetadata.ValidatorMetadata.Contains(attribute))
                {
                    context.ValidationMetadata.ValidatorMetadata.Add(attribute);
                }
            }
        }

        // IPropertyValidationFilter attributes on a type affect properties in that type, not properties that have
        // that type. Thus, we ignore context.TypeAttributes for properties and not check at all for types.
        if (context.Key.MetadataKind == ModelMetadataKind.Property)
        {
            var validationFilter = context.PropertyAttributes!.OfType<IPropertyValidationFilter>().FirstOrDefault();
            if (validationFilter == null)
            {
                // No IPropertyValidationFilter attributes on the property.
                // Check if container has such an attribute.
                validationFilter = context.Key.ContainerType!
                    .GetCustomAttributes(inherit: true)
                    .OfType<IPropertyValidationFilter>()
                    .FirstOrDefault();
            }

            context.ValidationMetadata.PropertyValidationFilter = validationFilter;
        }
        else if (context.Key.MetadataKind == ModelMetadataKind.Parameter)
        {
            var validationFilter = context.ParameterAttributes!.OfType<IPropertyValidationFilter>().FirstOrDefault();
            context.ValidationMetadata.PropertyValidationFilter = validationFilter;
        }
    }
}
