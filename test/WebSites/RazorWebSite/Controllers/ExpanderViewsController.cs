// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace RazorWebSite.Controllers
{
    public class ExpanderViewsController : Controller
    {
        // This result discovers the Index.cshtml from /View but the partial is executed from /Shared-Views
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Partial()
        {
            return PartialView("_ExpanderPartial");
        }
    }
}