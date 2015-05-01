// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace RazorWebSite
{
    public class FlushPoint : Controller
    {
        public ViewResult PageWithLayout()
        {
            return View();
        }

        public ViewResult PageWithoutLayout()
        {
            return View();
        }

        // This uses RenderSection to render the section that contains a FlushAsync call
        public ViewResult PageWithPartialsAndViewComponents()
        {
            return View();
        }

        // This uses RenderSectionAsync to render the section that contains a FlushAsync call
        public ViewResult PageWithRenderSectionAsync()
        {
            return View("PageWithSectionInvokedViaRenderSectionAsync");
        }

        public ViewResult PageWithNestedLayout()
        {
            return View();
        }

        public ViewResult PageWithFlushBeforeLayout()
        {
            return View();
        }
    }
}