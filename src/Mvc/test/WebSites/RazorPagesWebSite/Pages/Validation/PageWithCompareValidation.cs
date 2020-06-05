// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite
{
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
}
