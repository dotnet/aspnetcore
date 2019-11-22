// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations
{
    internal class ValidatableObjectAdapter : IModelValidator
    {
        public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
        {
            var model = context.Model;
            if (model == null)
            {
                return Enumerable.Empty<ModelValidationResult>();
            }

            if (!(model is IValidatableObject validatable))
            {
                var message = Resources.FormatValidatableObjectAdapter_IncompatibleType(
                    typeof(IValidatableObject).Name,
                    model.GetType());

                throw new InvalidOperationException(message);
            }

            // The constructed ValidationContext is intentionally slightly different from what
            // DataAnnotationsModelValidator creates. The instance parameter would be context.Container
            // (if non-null) in that class. But, DataAnnotationsModelValidator _also_ passes context.Model
            // separately to any ValidationAttribute.
            var validationContext = new ValidationContext(
                instance: validatable,
                serviceProvider: context.ActionContext?.HttpContext?.RequestServices,
                items: null)
            {
                DisplayName = context.ModelMetadata.GetDisplayName(),
                MemberName = context.ModelMetadata.Name,
            };

            return ConvertResults(validatable.Validate(validationContext));
        }

        private IEnumerable<ModelValidationResult> ConvertResults(IEnumerable<ValidationResult> results)
        {
            foreach (var result in results)
            {
                if (result != ValidationResult.Success)
                {
                    if (result.MemberNames == null || !result.MemberNames.Any())
                    {
                        yield return new ModelValidationResult(memberName: null, message: result.ErrorMessage);
                    }
                    else
                    {
                        foreach (var memberName in result.MemberNames)
                        {
                            yield return new ModelValidationResult(memberName, result.ErrorMessage);
                        }
                    }
                }
            }
        }
    }
}
