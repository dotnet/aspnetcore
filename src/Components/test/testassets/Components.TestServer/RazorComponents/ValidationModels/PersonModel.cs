// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

namespace BasicTestApp.ValidationModels;
#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
[ValidatableType]
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
public class Person : IValidatableObject
{
    [Required]
    public bool IsACat { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Under-zeros should not be filling out forms")]
    public int AgeInYears { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var minAge = IsACat ? 3 : 18;
        if (AgeInYears < minAge)
        {
            // Supply a model-level error (i.e., not attached to a specific property)
            yield return new ValidationResult($"Sorry, you're not old enough as a {(IsACat ? "cat" : "non-cat")}");
        }
    }
}
