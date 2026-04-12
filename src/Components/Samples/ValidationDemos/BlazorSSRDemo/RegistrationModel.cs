// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace BlazorSSRDemo;

public class RegistrationModel
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    [Display(Name = "Full Name")]
    public string Name { get; set; } = "";

    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required]
    [Compare("Password")]
    [Display(Name = "Confirm Password")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";

    [Range(18, 120)]
    public int? Age { get; set; }

    [Phone]
    [Display(Name = "Phone Number")]
    public string? Phone { get; set; }

    [RegularExpression(@"\d{5}(-\d{4})?", ErrorMessage = "Enter a valid US zip code.")]
    [Display(Name = "Zip Code")]
    public string? ZipCode { get; set; }
}
