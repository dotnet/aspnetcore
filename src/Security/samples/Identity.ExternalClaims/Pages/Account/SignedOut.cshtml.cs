// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class SignedOutModel : PageModel
{
    public IActionResult OnGet()
    {
        if (User.Identity.IsAuthenticated)
        {
            // Redirect to home page if the user is authenticated.
            return RedirectToPage("/Index");
        }

        return Page();
    }
}
