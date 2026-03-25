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

    [Required]
    [Range(18, 120)]
    public int? Age { get; set; }

    [Required]
    [MinLength(4, ErrorMessage = "Password must be at least 4 characters long.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = default!;

    [Required]
    [Compare(nameof(Password))]
    [Display(Name = "Confirm Password")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = default!;
}
