// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace BlazorUnitedApp.Data;

#pragma warning disable ASP0029
[Microsoft.Extensions.Validation.ValidatableType]
#pragma warning restore ASP0029
public class Registration
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [UniqueEmail]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Username is required.")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "Username must be 3–20 characters.")]
    [UniqueUsername]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Display name is required.")]
    public string DisplayName { get; set; } = "";
}
