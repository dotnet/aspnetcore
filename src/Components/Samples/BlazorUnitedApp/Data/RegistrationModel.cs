// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace BlazorUnitedApp.Data;

// Exercises a broad range of built-in DataAnnotations so the client-side validation rework can be
// tested by hand: required, length, email, phone, range, credit card, url, regex and compare all
// map to a corresponding JS validator.
public class RegistrationModel
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(20, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 20 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required.")]
    [Phone(ErrorMessage = "Enter a valid phone number.")]
    public string Phone { get; set; } = string.Empty;

    [Range(18, 120, ErrorMessage = "Age must be between 18 and 120.")]
    public int Age { get; set; }

    [Required(ErrorMessage = "Credit card is required.")]
    [CreditCard(ErrorMessage = "Enter a valid credit card number.")]
    public string CreditCard { get; set; } = string.Empty;

    [Url(ErrorMessage = "Enter a valid URL.")]
    public string Website { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = string.Empty;

    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
