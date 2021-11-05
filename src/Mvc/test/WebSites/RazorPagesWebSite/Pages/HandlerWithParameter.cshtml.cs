// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite.Pages;

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
