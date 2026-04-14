// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using BlazorNet10.Resources;

namespace BlazorNet10.Data;

public class ContactModel
{
    // No localization:
    // CLR property name and default English message is used.

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = "";

    // Static localization:
    // Display name and error message via static resource properties.

    [Display(ResourceType = typeof(LocalizedStrings), Name = "ContactEmail")]
    [Required(ErrorMessageResourceType = typeof(LocalizedStrings), ErrorMessageResourceName = "RequiredError")]
    [EmailAddress(ErrorMessageResourceType = typeof(LocalizedStrings), ErrorMessageResourceName = "EmailError")]
    public string Email { get; set; } = "";

    // Key-based localization:
    // By default, display name and error messages are used as hardcoded literals.
    // We want to use them as resource keys that can be looked up via IStringLocalizer at runtime.

    [Display(Name = "Message")]
    [Required(ErrorMessage = "RequiredError")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "StringLengthError")]
    public string Message { get; set; } = "";
}
