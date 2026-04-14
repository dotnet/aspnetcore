// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using BlazorNet11.Validators;

namespace BlazorNet11.Data;

[ValidatableType]
public class RegistrationModel
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    [Display(Name = "Full Name")]
    public string Name { get; set; } = "";

    [Required]
    [EmailAddress]
    [UniqueEmail]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Required]
    [UniqueUsername]
    [Display(Name = "Username")]
    public string Username { get; set; } = "";

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
}
