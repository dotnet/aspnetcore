// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace MvcValidationDemo.Models;

public class RegistrationModel
{
    [Required]
    [Display(Name = "Full Name")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = "";

    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    [Phone]
    [Display(Name = "Phone Number")]
    public string Phone { get; set; } = "";

    [Required]
    [Url]
    public string Website { get; set; } = "";

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

    [CreditCard]
    [Display(Name = "Credit Card")]
    public string? CreditCard { get; set; }

    [RegularExpression(@"\d{5}(-\d{4})?", ErrorMessage = "Enter a valid US zip code (e.g. 12345 or 12345-6789).")]
    [Display(Name = "Zip Code")]
    public string? ZipCode { get; set; }

    [FileExtensions(Extensions = "png,jpg,jpeg,gif")]
    [Display(Name = "Avatar Filename")]
    public string? AvatarFilename { get; set; }
}
