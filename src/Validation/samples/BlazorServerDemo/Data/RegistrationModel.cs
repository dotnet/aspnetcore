// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0029

using System.ComponentModel.DataAnnotations;

namespace BlazorServerDemo.Data;

[Microsoft.Extensions.Validation.ValidatableType]
public class RegistrationModel
{
    [Required(ErrorMessage = "RequiredError")]
    [EmailAddress(ErrorMessage = "EmailError")]
    [UniqueEmail(ErrorMessage = "UniqueEmailError")]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "RequiredError")]
    [StringLength(20, MinimumLength = 4, ErrorMessage = "StringLengthError")]
    [UniqueUsername(ErrorMessage = "UniqueUsernameError")]
    [Display(Name = "Username")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "RequiredError")]
    [Range(13, 120, ErrorMessage = "RangeError")]
    [Display(Name = "Age")]
    public int? Age { get; set; }

    [Url(ErrorMessage = "UrlError")]
    [Display(Name = "Website")]
    public string? Website { get; set; }
}
