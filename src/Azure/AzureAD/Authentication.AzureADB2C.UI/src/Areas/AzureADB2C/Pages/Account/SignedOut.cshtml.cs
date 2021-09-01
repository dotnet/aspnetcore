// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Authentication.AzureADB2C.UI.Internal
{
    /// <summary>
    /// This API supports infrastructure and is not intended to be used
    /// directly from your code.This API may change or be removed in future releases
    /// </summary>
    [AllowAnonymous]
    public class SignedOutModel : PageModel
    {
        /// <summary>
        /// This API supports infrastructure and is not intended to be used
        /// directly from your code.This API may change or be removed in future releases
        /// </summary>
        public IActionResult OnGet()
        {
            if (User.Identity.IsAuthenticated)
            {
                return LocalRedirect("~/");
            }

            return Page();
        }
    }
}