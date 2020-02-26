// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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