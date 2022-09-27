// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

internal sealed class ValidatableObjectAdapter : IModelValidator
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

    private static IEnumerable<ModelValidationResult> ConvertResults(IEnumerable<ValidationResult> results)
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
