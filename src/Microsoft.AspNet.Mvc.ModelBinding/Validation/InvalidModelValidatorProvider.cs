// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class InvalidModelValidatorProvider : AssociatedValidatorProvider
    {
        protected override IEnumerable<IModelValidator> GetValidators(ModelMetadata metadata,
                                                                      IEnumerable<Attribute> attributes)
        {
            if (metadata.ContainerType == null || string.IsNullOrEmpty(metadata.PropertyName))
            {
                // Validate that the type's fields and nonpublic properties don't have any validation attributes on 
                // them. Validation only runs against public properties
                var type = metadata.ModelType;
                var nonPublicProperties = type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var nonPublicProperty in nonPublicProperties)
                {
                    if (nonPublicProperty.GetCustomAttributes(typeof(ValidationAttribute), inherit: true).Any())
                    {
                        var message = Resources.FormatValidationAttributeOnNonPublicProperty(nonPublicProperty.Name,
                                                                                             type);
                        yield return new ErrorModelValidator(message);
                    }
                }

                var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                var allFields = metadata.ModelType.GetFields(bindingFlags);
                foreach (var field in allFields)
                {
                    if (field.GetCustomAttributes(typeof(ValidationAttribute), inherit: true).Any())
                    {
                        var message = Resources.FormatValidationAttributeOnField(field.Name, type);
                        yield return new ErrorModelValidator(message);
                    }
                }
            }
            else
            {
                // Validate that value-typed properties marked as [Required] are also marked as
                // [DataMember(IsRequired=true)]. Certain formatters may not recognize a member as required if it's
                // marked as [Required] but not [DataMember(IsRequired=true)]. This is not a problem for reference
                // types because [Required] will still cause a model error to be raised after a null value is 
                // deserialized.
                if (metadata.ModelType.GetTypeInfo().IsValueType &&
                    attributes.Any(attribute => attribute is RequiredAttribute))
                {
                    if (!DataMemberModelValidatorProvider.IsRequiredDataMember(metadata.ContainerType, attributes))
                    {
                        var message = Resources.FormatMissingDataMemberIsRequired(metadata.PropertyName,
                                                                                  metadata.ContainerType);
                        yield return new ErrorModelValidator(message);
                    }
                }
            }
        }
    }
}
