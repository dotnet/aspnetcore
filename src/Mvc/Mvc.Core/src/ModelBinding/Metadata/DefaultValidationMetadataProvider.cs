// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
{
    /// <summary>
    /// A default implementation of <see cref="IValidationMetadataProvider"/>.
    /// </summary>
    internal class DefaultValidationMetadataProvider : IValidationMetadataProvider
    {
        /// <inheritdoc />
        public void CreateValidationMetadata(ValidationMetadataProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

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
                var validationFilter = context.PropertyAttributes.OfType<IPropertyValidationFilter>().FirstOrDefault();
                if (validationFilter == null)
                {
                    // No IPropertyValidationFilter attributes on the property.
                    // Check if container has such an attribute.
                    validationFilter = context.Key.ContainerType.GetTypeInfo()
                        .GetCustomAttributes(inherit: true)
                        .OfType<IPropertyValidationFilter>()
                        .FirstOrDefault();
                }

                context.ValidationMetadata.PropertyValidationFilter = validationFilter;
            }
        }
    }
}
