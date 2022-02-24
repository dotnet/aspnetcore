// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SecurityWebSite;

public class AllowAnonymousPageViaConvention : PageModel
{
    public IActionResult OnGet() => Page();
}
