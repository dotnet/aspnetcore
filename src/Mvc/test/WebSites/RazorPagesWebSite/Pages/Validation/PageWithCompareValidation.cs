// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite;

public class PageWithCompareValidation : PageModel
{
    [BindProperty(SupportsGet = true)]
    [Required(ErrorMessage = "User name is required.")]
    public string UserName { get; set; }

    [BindProperty(SupportsGet = true)]
    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; }

    [BindProperty(SupportsGet = true)]
    [Compare(nameof(Password), ErrorMessage = "Password and confirm password do not match.")]
    public int ConfirmPassword { get; set; }

    [Required] // Here to make sure we do not validate an unbound property.
    public string NotBoundProperty { get; set; }
}
