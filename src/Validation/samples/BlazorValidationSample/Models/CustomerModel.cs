// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0029

using System.ComponentModel.DataAnnotations;
namespace BlazorValidationSample.Models;

/// <summary>
/// Represents a customer with validation attributes demonstrating
/// <see cref="Required"/>, <see cref="StringLength"/>, <see cref="EmailAddress"/>,
/// <see cref="Range"/>, <see cref="Phone"/>, and <see cref="Display"/> localization.
/// </summary>
[Microsoft.Extensions.Validation.ValidatableType]
public class CustomerModel
{
    [Required(ErrorMessage = "RequiredError")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "StringLengthError")]
    [Display(Name = "CustomerName")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "RequiredError")]
    [EmailAddress(ErrorMessage = "EmailError")]
    [Display(Name = "CustomerEmail")]
    public string? Email { get; set; }

    [Range(18, 120, ErrorMessage = "RangeError")]
    [Display(Name = "CustomerAge")]
    public int Age { get; set; }

    [Phone(ErrorMessage = "PhoneError")]
    [Display(Name = "CustomerPhone")]
    public string? Phone { get; set; }
}
