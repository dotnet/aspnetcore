// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace BlazorSSR.Models;

/// <summary>
/// A sample form model demonstrating various DataAnnotations validation attributes.
/// </summary>
public class ContactModel
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Age is required.")]
    [Range(18, 120, ErrorMessage = "Age must be between 18 and 120.")]
    public int? Age { get; set; }

    [Url(ErrorMessage = "Please enter a valid URL.")]
    public string? Website { get; set; }

    [Required(ErrorMessage = "A message is required.")]
    [MinLength(10, ErrorMessage = "Message must be at least 10 characters.")]
    [MaxLength(1000, ErrorMessage = "Message cannot exceed 1000 characters.")]
    public string Message { get; set; } = string.Empty;

    [RegularExpression(@"^[A-Z]{2}-\d{4}$", ErrorMessage = "Reference code must be in the format XX-0000.")]
    public string? ReferenceCode { get; set; }
}
