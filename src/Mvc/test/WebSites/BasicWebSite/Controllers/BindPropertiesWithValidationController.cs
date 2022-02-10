// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BasicWebSite;

[BindProperties(SupportsGet = true)]
public class BindPropertiesWithValidationController : Controller
{
    [Required(ErrorMessage = "User name is required.")]
    public string UserName { get; set; }

    [Required]
    public string Password { get; set; }

    [Compare(nameof(Password), ErrorMessage = "Password and confirm password do not match.")]
    public string ConfirmPassword { get; set; }

    [BindNever]
    public string BindNeverProperty { get; set; }

    public IActionResult Action()
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem();
        }

        return Ok();
    }
}
