// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace Mvc.RoutingWebSite.Controllers;

public class BranchesController : Controller
{
    private readonly TestResponseGenerator _generator;

    public BranchesController(TestResponseGenerator generator)
    {
        _generator = generator;
    }

    public IActionResult Index()
    {
        return _generator.Generate();
    }

    [HttpGet("dynamicattributeorder/{some}/{value}/{**slug}", Order = 1)]
    public IActionResult Attribute()
    {
        return _generator.Generate();
    }
}
