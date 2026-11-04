// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite;

public class ConventionalTransformerController : Controller
{
    private readonly TestResponseGenerator _generator;

    public ConventionalTransformerController(TestResponseGenerator generator)
    {
        _generator = generator;
    }

    public IActionResult Index()
    {
        return _generator.Generate();
    }

    public IActionResult Param(string param)
    {
        return _generator.Generate($"/ConventionalTransformerRoute/conventional-transformer/Param/{param}");
    }
}
