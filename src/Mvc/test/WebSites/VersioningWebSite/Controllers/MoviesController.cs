// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace VersioningWebSite;

// Scenario:
// This is a controller for the V1 (unconstrained) version
// of the service. The v2 version will be in a controller
// that contains only actions for which the api surface
// changes. Actions for which V1 and V2 have the same
// API surface.
public class MoviesController : Controller
{
    private readonly TestResponseGenerator _generator;

    public MoviesController(TestResponseGenerator generator)
    {
        _generator = generator;
    }

    [HttpGet("Movies")]
    public IActionResult Get()
    {
        return _generator.Generate();
    }

    [HttpGet("Movies/{id}")]
    public IActionResult GetById(int id)
    {
        return _generator.Generate();
    }

    [HttpPost("/Movies")]
    public IActionResult Post()
    {
        return _generator.Generate();
    }

    [HttpPut("Movies/{id}")]
    public IActionResult Put(int id)
    {
        return _generator.Generate();
    }

    [HttpDelete("Movies/{id}")]
    public IActionResult Delete(int id)
    {
        return _generator.Generate();
    }
}
