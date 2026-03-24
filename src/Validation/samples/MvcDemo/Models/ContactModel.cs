// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace MvcDemo.Models;

public class ContactModel
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = default!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = default!;

    [Phone]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [Url]
    [Display(Name = "Website")]
    public string? Website { get; set; }

    [Required]
    [Range(18, 120)]
    public int? Age { get; set; }

    [Required]
    [MinLength(8)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = default!;

    [Required]
    [Compare(nameof(Password))]
    [Display(Name = "Confirm Password")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = default!;
}
