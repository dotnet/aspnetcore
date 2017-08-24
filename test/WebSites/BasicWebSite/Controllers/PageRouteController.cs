// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers
{
    // Verifies that we can use the "page" token in routing in a controller only (no Razor Pages) application
    // without affecting view lookups.
    public class PageRouteController : Controller
    {
        public IActionResult ConventionalRoute(string page)
        {
            ViewData["page"] = page;
            return View();
        }

        [HttpGet("/PageRoute/Attribute/{page}")]
        public IActionResult AttributeRoute(string page)
        {
            ViewData["page"] = page;
            return View();
        }
    }
}
