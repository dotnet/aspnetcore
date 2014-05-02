// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class CompareAttributeAdapter : DataAnnotationsModelValidator<CompareAttribute>
    {
        public CompareAttributeAdapter([NotNull] CompareAttribute attribute)
            : base(new CompareAttributeWrapper(attribute))
        {
        }

        public override IEnumerable<ModelClientValidationRule> GetClientValidationRules(
            [NotNull] ClientModelValidationContext context)
        {
            var errorMessage = ((CompareAttributeWrapper)Attribute).FormatErrorMessage(context);
            var clientRule = new ModelClientValidationEqualToRule(errorMessage, 
                                                                  FormatPropertyForClientValidation(Attribute.OtherProperty));
            return new [] { clientRule };
        }

        private static string FormatPropertyForClientValidation(string property)
        {
            return "*." + property;
        }

        private sealed class CompareAttributeWrapper : CompareAttribute
        {
            public CompareAttributeWrapper(CompareAttribute attribute)
                : base(attribute.OtherProperty)
            {
                // Copy settable properties from wrapped attribute. Don't reset default message accessor (set as
                // CompareAttribute constructor calls ValidationAttribute constructor) when all properties are null to
                // preserve default error message. Reset the message accessor when just ErrorMessageResourceType is
                // non-null to ensure correct InvalidOperationException.
                if (!string.IsNullOrEmpty(attribute.ErrorMessage) ||
                    !string.IsNullOrEmpty(attribute.ErrorMessageResourceName) ||
                    attribute.ErrorMessageResourceType != null)
                {
                    ErrorMessage = attribute.ErrorMessage;
                    ErrorMessageResourceName = attribute.ErrorMessageResourceName;
                    ErrorMessageResourceType = attribute.ErrorMessageResourceType;
                }
            }

            public string FormatErrorMessage(ClientModelValidationContext context)
            {
                var displayName = context.ModelMetadata.GetDisplayName();
                return string.Format(CultureInfo.CurrentCulture, 
                                     ErrorMessageString, 
                                     displayName, 
                                     GetOtherPropertyDisplayName(context));
            }

            private string GetOtherPropertyDisplayName(ClientModelValidationContext context)
            {
                // The System.ComponentModel.DataAnnotations.CompareAttribute doesn't populate the OtherPropertyDisplayName
                // until after IsValid() is called. Therefore, by the time we get the error message for client validation, 
                // the display name is not populated and won't be used.
                var metadata = context.ModelMetadata;
                var otherPropertyDisplayName = OtherPropertyDisplayName;
                if (otherPropertyDisplayName == null && metadata.ContainerType != null)
                {
                    var otherProperty = context.MetadataProvider
                                               .GetMetadataForProperty(() => metadata.Model,
                                                                       metadata.ContainerType,
                                                                       OtherProperty);
                    if (otherProperty != null)
                    {
                        return otherProperty.GetDisplayName();
                    }
                }

                return OtherProperty;
            }
        }
    }
}