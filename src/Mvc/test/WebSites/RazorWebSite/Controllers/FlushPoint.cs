// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace RazorWebSite
{
    public class FlushPoint : Controller
    {
        public IActionResult PageWithLayout()
        {
            return View();
        }

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
}