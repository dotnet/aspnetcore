// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public class DataAnnotationsModelValidator : IModelValidator
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
            var modelExplorer = validationContext.ModelExplorer;
            var metadata = modelExplorer.Metadata;

            var memberName = metadata.PropertyName ?? metadata.ModelType.Name;
            var containerExplorer = modelExplorer.Container;

            var container = containerExplorer?.Model;
            var context = new ValidationContext(container ?? modelExplorer.Model)
            {
                DisplayName = metadata.GetDisplayName(),
                MemberName = memberName
            };

            var result = Attribute.GetValidationResult(modelExplorer.Model, context);
            if (result != ValidationResult.Success)
            {
                // ModelValidationResult.MemberName is used by invoking validators (such as ModelValidator) to
                // construct the ModelKey for ModelStateDictionary. When validating at type level we want to append
                // the returned MemberNames if specified (e.g. person.Address.FirstName). For property validation, the
                // ModelKey can be constructed using the ModelMetadata and we should ignore MemberName (we don't want
                // (person.Name.Name). However the invoking validator does not have a way to distinguish between these
                // two cases. Consequently we'll only set MemberName if this validation returns a MemberName that is
                // different from the property being validated.

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
    }
}
