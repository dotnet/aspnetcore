// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BasicWebSite
{
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
}
