// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite.Pages
{
    public class HandlerWithParameterModel : PageModel
    {
        public IActionResult OnGet(string testParameter = null)
        {
            if (testParameter == null)
            {
                return BadRequest("Parameter cannot be null.");
            }

            return Page();
        }
    }
}
