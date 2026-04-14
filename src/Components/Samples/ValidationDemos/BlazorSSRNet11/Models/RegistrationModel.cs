// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace BlazorSSRNet11.Models;

public class RegistrationModel
{
    [Required(ErrorMessage = "RequiredError")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "StringLengthError")]
    [Display(Name = "FullName")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "RequiredError")]
    [EmailAddress(ErrorMessage = "EmailError")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "RequiredError")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "StringLengthError")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "RequiredError")]
    [Compare("Password", ErrorMessage = "CompareError")]
    [Display(Name = "ConfirmPassword")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";

    [Range(18, 120, ErrorMessage = "RangeError")]
    public int? Age { get; set; }

    [Phone(ErrorMessage = "PhoneError")]
    [Display(Name = "PhoneNumber")]
    public string? Phone { get; set; }

    [RegularExpression(@"\d{5}(-\d{4})?", ErrorMessage = "RegexError")]
    [Display(Name = "ZipCode")]
    public string? ZipCode { get; set; }
}
