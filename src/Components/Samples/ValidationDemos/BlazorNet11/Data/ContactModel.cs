// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using BlazorNet11.Resources;

namespace BlazorNet11.Data;

[ValidatableType]
public class ContactModel
{
    // No localization:
    // CLR property name and default English message is used.

    [Required]
    public string Name { get; set; } = "";

    // Static localization:
    // Display name and error message via static resource properties.

    [Display(ResourceType = typeof(LocalizedStrings), Name = nameof(LocalizedStrings.ContactEmail))]
    [Required(ErrorMessageResourceType = typeof(LocalizedStrings), ErrorMessageResourceName = nameof(LocalizedStrings.RequiredError))]
    [EmailAddress(ErrorMessageResourceType = typeof(LocalizedStrings), ErrorMessageResourceName = nameof(LocalizedStrings.EmailError))]
    public string Email { get; set; } = "";

    // Key-based localization:
    // By default, display name and error messages are used as hardcoded literals.
    // We want to use them as resource keys that can be looked up via IStringLocalizer at runtime.

    [Display(Name = "Message")]
    [Required(ErrorMessage = "RequiredError")]
    [StringLength(1000, MinimumLength = 5, ErrorMessage = "StringLengthError")]
    public string Message { get; set; } = "";
}
