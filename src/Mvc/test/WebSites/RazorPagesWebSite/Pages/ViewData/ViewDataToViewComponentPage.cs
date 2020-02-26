// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorPagesWebSite.Components;

namespace RazorPagesWebSite
{
    public class ViewDataToViewComponentPage : PageModel
    {
        [ViewData]
        public string Title => "View Data in Pages";

        [ViewData]
        public string Message { get; private set; }

        public IActionResult OnGet()
        {
            Message = "Message set in handler";
            return new ViewComponentResult
            {
                ViewComponentType = typeof(ViewDataViewComponent),
            };
        }
    }
}
