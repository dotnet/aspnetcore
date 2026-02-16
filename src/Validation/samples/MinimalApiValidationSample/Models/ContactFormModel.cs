// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace MinimalApiValidationSample.Models;

/// <summary>
/// Represents a contact form submission. Uses custom <c>ErrorMessage</c> keys
/// (e.g., <c>"RequiredError"</c>, <c>"LengthError"</c>) that are resolved by the
/// configured <see cref="Microsoft.Extensions.Localization.IStringLocalizer"/> to
/// produce culture-specific validation messages.
/// </summary>
public class ContactFormModel
{
    /// <summary>
    /// Gets or sets the contact name.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "LengthError")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the contact email address.
    /// </summary>
    [Required(ErrorMessage = "RequiredError")]
    [EmailAddress(ErrorMessage = "EmailInvalid")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the contact phone number.
    /// </summary>
    [Required(ErrorMessage = "RequiredError")]
    [Phone(ErrorMessage = "PhoneInvalid")]
    [Display(Name = "Phone")]
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message content.
    /// </summary>
    [Required(ErrorMessage = "RequiredError")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "LengthError")]
    [Display(Name = "Message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the contact's age. Must be between 18 and 120.
    /// </summary>
    [Range(18, 120, ErrorMessage = "RangeError")]
    [Display(Name = "Age")]
    public int? Age { get; set; }
}
