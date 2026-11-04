// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RazorWebSite;

public class FlushPoint : Controller
{
    public IActionResult PageWithLayout()
    {
        return View();
    }

    public IActionResult FlushFollowedByLargeContent() => View();

    public IActionResult FlushInvokedInComponent() => View();

    public IActionResult PageWithoutLayout()
    {
        return View();
    }

    // This uses RenderSection to render the section that contains a FlushAsync call
    public IActionResult PageWithPartialsAndViewComponents()
    {
        return View();
    }

    // This uses RenderSectionAsync to render the section that contains a FlushAsync call
    public IActionResult PageWithRenderSectionAsync()
    {
        return View("PageWithSectionInvokedViaRenderSectionAsync");
    }

    public IActionResult PageWithNestedLayout()
    {
        return View();
    }

    public IActionResult PageWithFlushBeforeLayout()
    {
        return View();
    }
}
