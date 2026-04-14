// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using BlazorInteractiveDemo.Resources;

namespace BlazorInteractiveDemo.Data;

/// <summary>
/// Contact form model demonstrating three localization approaches side by side:
/// 1. No localization — bare attributes, default English messages
/// 2. Static resources — ResourceType/ResourceName on the attribute
/// 3. IStringLocalizer — ErrorMessage used as a lookup key
/// </summary>
public class ContactModel
{
    // --- Approach 1: No localization (default English messages) ---

    /// <summary>No Display attribute, no ErrorMessage — uses CLR property name and default messages.</summary>
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = "";

    // --- Approach 2: Static resource localization (ResourceType/ResourceName) ---

    /// <summary>Display name and error message via static resource properties.</summary>
    [Required(ErrorMessageResourceType = typeof(StaticResources), ErrorMessageResourceName = nameof(StaticResources.PhoneRequired))]
    [Phone]
    [Display(ResourceType = typeof(StaticResources), Name = nameof(StaticResources.PhoneLabel))]
    public string Phone { get; set; } = "";

    // --- Approach 3: IStringLocalizer-based localization (ErrorMessage as lookup key) ---

    /// <summary>ErrorMessage is a resource key looked up via IStringLocalizer at runtime.</summary>
    [Required(ErrorMessage = "Required")]
    [EmailAddress(ErrorMessage = "EmailInvalid")]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";

    /// <summary>ErrorMessage is a resource key looked up via IStringLocalizer at runtime.</summary>
    [Required(ErrorMessage = "Required")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "StringLength")]
    [Display(Name = "Message")]
    public string Message { get; set; } = "";
}
