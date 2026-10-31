// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace VersioningWebSite;

// Scenario
// V1 of the API is read-only and unconstrained
// V2 of the API is constrained
public class TicketsController : Controller
{
    private readonly TestResponseGenerator _generator;

    public TicketsController(TestResponseGenerator generator)
    {
        _generator = generator;
    }

    [HttpGet("/Tickets")]
    public IActionResult Get()
    {
        return _generator.Generate();
    }

    [HttpGet("/Tickets/{id}")]
    public IActionResult GetById(int id)
    {
        return _generator.Generate();
    }

    [VersionPost("/Tickets", versionRange: "2")]
    public IActionResult Post()
    {
        return _generator.Generate();
    }

    [VersionPut("/Tickets/{id}", versionRange: "2")]
    public IActionResult Put(int id)
    {
        return _generator.Generate();
    }

    [VersionDelete("/Tickets/{id}", versionRange: "2")]
    public IActionResult Delete(int id)
    {
        return _generator.Generate();
    }
}
