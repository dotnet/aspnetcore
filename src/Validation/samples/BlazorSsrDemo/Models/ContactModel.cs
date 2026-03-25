// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace BlazorSsrDemo.Models;

/// <summary>
/// Contact form model with DataAnnotations validation and localized display names.
/// </summary>
#pragma warning disable ASP0029 // ValidatableTypeAttribute is experimental
[Microsoft.Extensions.Validation.ValidatableType]
#pragma warning restore ASP0029
public class ContactModel
{
    [Required(ErrorMessage = "RequiredError")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "StringLengthError")]
    [Display(Name = "ContactName")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "RequiredError")]
    [EmailAddress(ErrorMessage = "EmailError")]
    [Display(Name = "ContactEmail")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "RequiredError")]
    [Phone(ErrorMessage = "PhoneError")]
    [Display(Name = "ContactPhone")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "RequiredError")]
    [Range(18, 120, ErrorMessage = "RangeError")]
    [Display(Name = "ContactAge")]
    public int? Age { get; set; }

    [Required(ErrorMessage = "RequiredError")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "StringLengthError")]
    [Display(Name = "ContactMessage")]
    public string? Message { get; set; }
}
