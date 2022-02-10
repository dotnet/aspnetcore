// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite;

public class PageRouteController
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
}
