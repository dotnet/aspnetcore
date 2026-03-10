// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace BlazorSSR.Models;

/// <summary>
/// A simple model for a newsletter subscribe form, used to test enhanced navigation
/// with a second form added to the page.
/// </summary>
public class SubscribeModel
{
    [Required(ErrorMessage = "Your name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;
}
