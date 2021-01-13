// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers
{
    // Verifies that we can use the "page" token in routing in a controller only (no Razor Pages) application
    // without affecting view lookups.
    public class PageRouteController : Controller
    {
        private readonly TestResponseGenerator _generator;

        public PageRouteController(TestResponseGenerator generator)
        {
            _generator = generator;
        }

        public IActionResult ConventionalRoute(string page)
        {
            return _generator.Generate("/PageRoute/ConventionalRoute/" + page);
        }

        [HttpGet("/PageRoute/Attribute/{page}")]
        public IActionResult AttributeRoute(string page)
        {
            return _generator.Generate("/PageRoute/Attribute/" + page);
        }

        public IActionResult ConventionalRouteView(string page)
        {
            ViewData["page"] = page;
            return View();
        }

        [HttpGet("/PageRoute/AttributeView/{page}")]
        public IActionResult AttributeRouteView(string page)
        {
            ViewData["page"] = page;
            return View();
        }
    }
}
