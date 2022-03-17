// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite;

[Area("Travel")]
[Route("ContosoCorp/Trains")]
public class RailController
{
    private readonly TestResponseGenerator _generator;

    public RailController(TestResponseGenerator generator)
    {
        _generator = generator;
    }

    public IActionResult Index()
    {
        return _generator.Generate("/ContosoCorp/Trains");
    }

    [HttpGet("CheckSchedule")]
    public IActionResult Schedule()
    {
        return _generator.Generate("/ContosoCorp/Trains/Schedule");
    }
}
