// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace FormatterWebSite.Models;

public class InvalidModel : IValidatableObject
{
    [Required]
    public string Name { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        yield return new ValidationResult("The model is not valid.");
    }
}
