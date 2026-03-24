// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace BlazorServerDemo.Data;

#pragma warning disable ASP0029
[Microsoft.Extensions.Validation.ValidatableType]
#pragma warning restore ASP0029
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
    /// Gets or sets the user's display name.
    /// </summary>
    [Required(ErrorMessage = "RequiredError")]
    [Display(Name = "DisplayName")]
    public string DisplayName { get; set; } = "";

    /// <summary>
    /// Gets or sets the user's age.
    /// </summary>
    [Range(18, 120, ErrorMessage = "RangeError")]
    [Display(Name = "Age")]
    public int? Age { get; set; }

    /// <summary>
    /// Gets or sets the user's website URL (optional).
    /// </summary>
    [Url(ErrorMessage = "UrlError")]
    [Display(Name = "Website")]
    public string? Website { get; set; }
}
