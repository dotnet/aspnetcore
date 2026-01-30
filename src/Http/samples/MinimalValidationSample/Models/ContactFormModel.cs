// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace MinimalValidationSample.Models;

public class ContactFormModel
{
    [Required]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "LengthError")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "RequiredError")]
    [EmailAddress(ErrorMessage = "EmailInvalid")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "RequiredError")]
    [Phone(ErrorMessage = "PhoneInvalid")]
    [Display(Name = "Phone")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "RequiredError")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "LengthError")]
    [Display(Name = "Message")]
    public string Message { get; set; } = string.Empty;

    [Range(18, 120, ErrorMessage = "RangeError")]
    [Display(Name = "Age")]
    public int? Age { get; set; }
}
