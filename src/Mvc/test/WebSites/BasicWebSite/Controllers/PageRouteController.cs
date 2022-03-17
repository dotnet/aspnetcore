// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers;

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
