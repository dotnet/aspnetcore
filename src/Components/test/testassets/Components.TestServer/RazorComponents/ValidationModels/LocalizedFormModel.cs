// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace BasicTestApp.ValidationModels;

[Microsoft.Extensions.Validation.ValidatableType]
public class LocalizedFormModel
{
    [Required(ErrorMessage = "RequiredKey")]
    [Display(Name = "Email Address")]
    public string? Email { get; set; }

    [Range(18, 120, ErrorMessage = "RangeKey")]
    [Display(Name = "Age")]
    public int Age { get; set; }
}
