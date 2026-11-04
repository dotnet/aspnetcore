// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorPagesWebSite.Components;

namespace RazorPagesWebSite;

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
