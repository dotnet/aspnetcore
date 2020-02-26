// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite
{
    public class RedirectFromModel : PageModel
    {
        public IActionResult OnGet() => RedirectToPage("/Pages/Redirects/Redirect", new { id = 12});
    }
}
