// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite.Pages.Filters
{
    [TestPageModelFilter]
    public class FiltersAppliedToPageAndPageModel : PageModel
    {
        public IActionResult OnGet() => Page();
    }
}
