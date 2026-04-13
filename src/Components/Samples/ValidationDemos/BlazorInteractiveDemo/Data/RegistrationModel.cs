// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using BlazorInteractiveDemo.Validators;

namespace BlazorInteractiveDemo.Data;

public class RegistrationModel
{
    [Required(ErrorMessage = "Required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "StringLength")]
    [Display(Name = "Full Name")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Required")]
    [EmailAddress(ErrorMessage = "Email")]
    [UniqueEmail]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Required")]
    [UniqueUsername]
    [Display(Name = "Username")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "StringLength")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Range(18, 120, ErrorMessage = "Range")]
    public int? Age { get; set; }
}
