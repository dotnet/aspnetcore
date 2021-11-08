// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace VersioningWebSite;

// Scenario
// Actions define version ranges and some
// versions overlap.
public class BooksController : Controller
{
    private readonly TestResponseGenerator _generator;

    public BooksController(TestResponseGenerator generator)
    {
        _generator = generator;
    }

    [VersionGet("Books", versionRange: "[1-6]", Order = 1)]
    public IActionResult Get()
    {
        return _generator.Generate();
    }

    [VersionGet("Books", versionRange: "[3-5]", Order = 0)]
    public IActionResult GetBreakingChange()
    {
        return _generator.Generate();
    }
}
