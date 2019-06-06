// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SecurityWebSite
{
    [AllowAnonymous]
    public class AllowAnonymousPageViaModel : PageModel
    {
        public IActionResult OnGet() => Page();
    }
}
