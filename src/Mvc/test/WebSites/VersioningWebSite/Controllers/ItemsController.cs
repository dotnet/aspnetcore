// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace VersioningWebSite;

// Scenario:
// Controller without any kind of specific version handling.
// New versions of the API will be exposed in a different controller.
[Route("Items/{id}")]
public class ItemsController : Controller
{
    private readonly TestResponseGenerator _generator;

    public ItemsController(TestResponseGenerator generator)
    {
        _generator = generator;
    }

    [HttpGet("/Items")]
    public IActionResult Get()
    {
        return _generator.Generate();
    }

    [HttpGet]
    public IActionResult Get(int id)
    {
        return _generator.Generate();
    }

    [HttpPost("/Items")]
    public IActionResult Post()
    {
        return _generator.Generate();
    }

    [HttpPut]
    public IActionResult Put(int id)
    {
        return _generator.Generate();
    }

    [HttpDelete]
    public IActionResult Delete(int id)
    {
        return _generator.Generate();
    }
}
