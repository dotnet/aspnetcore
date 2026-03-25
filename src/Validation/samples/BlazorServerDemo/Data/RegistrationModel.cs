// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0029

using System.ComponentModel.DataAnnotations;

namespace BlazorServerDemo.Data;

[Microsoft.Extensions.Validation.ValidatableType]
public class RegistrationModel
{
    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    [Required(ErrorMessage = "RequiredError")]
    [EmailAddress(ErrorMessage = "EmailError")]
    [UniqueEmail]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";

    /// <summary>
    /// Gets or sets the desired username.
    /// </summary>
    [Required(ErrorMessage = "RequiredError")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "StringLengthError")]
    [UniqueUsername]
    [Display(Name = "Username")]
    public string Username { get; set; } = "";

    /// <summary>
    /// Gets or sets the user's age.
    /// </summary>
    [Required(ErrorMessage = "RequiredError")]
    [Range(13, 120, ErrorMessage = "RangeError")]
    [Display(Name = "Age")]
    public int? Age { get; set; }

    /// <summary>
    /// Gets or sets the user's website URL (optional).
    /// </summary>
    [Url(ErrorMessage = "UrlError")]
    [Display(Name = "Website")]
    public string? Website { get; set; }
}
