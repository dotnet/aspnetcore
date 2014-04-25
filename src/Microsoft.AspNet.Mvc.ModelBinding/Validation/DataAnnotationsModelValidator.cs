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

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class DataAnnotationsModelValidator : IModelValidator, IClientModelValidator
    {
        public DataAnnotationsModelValidator([NotNull] ValidationAttribute attribute)
        {
            Attribute = attribute;
        }

        public ValidationAttribute Attribute { get; private set; }

        public bool IsRequired
        {
            get { return Attribute is RequiredAttribute; }
        }

        public IEnumerable<ModelValidationResult> Validate(ModelValidationContext validationContext)
        {
            var metadata = validationContext.ModelMetadata;
            var memberName = metadata.PropertyName ?? metadata.ModelType.Name;
            var containerMetadata = validationContext.ContainerMetadata;
            var container = containerMetadata != null ? containerMetadata.Model : null;
            var context = new ValidationContext(container ?? metadata.Model)
            {
                DisplayName = metadata.GetDisplayName(),
                MemberName = memberName
            };

            var result = Attribute.GetValidationResult(metadata.Model, context);
            if (result != ValidationResult.Success)
            {
                // ModelValidationResult.MemberName is used by invoking validators (such as ModelValidator) to 
                // construct the ModelKey for ModelStateDictionary. When validating at type level we want to append the 
                // returned MemberNames if specified (e.g. person.Address.FirstName). For property validation, the 
                // ModelKey can be constructed using the ModelMetadata and we should ignore MemberName (we don't want 
                // (person.Name.Name). However the invoking validator does not have a way to distinguish between these two 
                // cases. Consequently we'll only set MemberName if this validation returns a MemberName that is different
                // from the property being validated.

                var errorMemberName = result.MemberNames.FirstOrDefault();
                if (string.Equals(errorMemberName, memberName, StringComparison.Ordinal))
                {
                    errorMemberName = null;
                }

                var validationResult = new ModelValidationResult(errorMemberName, result.ErrorMessage);
                return new ModelValidationResult[] { validationResult };
            }

            return Enumerable.Empty<ModelValidationResult>();
        }

        public virtual IEnumerable<ModelClientValidationRule> GetClientValidationRules(
            [NotNull] ClientModelValidationContext context)
        {
            return Enumerable.Empty<ModelClientValidationRule>();
        }

        protected virtual string GetErrorMessage([NotNull] ModelMetadata modelMetadata)
        {
            return Attribute.FormatErrorMessage(modelMetadata.GetDisplayName());
        }
    }
}
