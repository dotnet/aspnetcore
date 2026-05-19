// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace VersioningWebSite;

// Scenario:
// Version constraint provided separately from the attribute route.
[Route("Customers")]
public class CustomersController
{
    private readonly TestResponseGenerator _generator;

    public CustomersController(TestResponseGenerator generator)
    {
        _generator = generator;
    }

    [HttpGet("{id}")]
    [Version(MaxVersion = 2)]
    public IActionResult Get(int id)
    {
        return _generator.Generate();
    }

    [HttpGet("{id}")]
    [Version(MinVersion = 3, MaxVersion = 5)]
    public IActionResult GetV3ToV5(int id)
    {
        return _generator.Generate();
    }

    [Version(MinVersion = 2)]
    public IActionResult AnyV2OrHigher()
    {
        return _generator.Generate();
    }

    [HttpPost]
    public IActionResult Post()
    {
        return _generator.Generate();
    }

    [Version(MinVersion = 2, Order = int.MaxValue)]
    [Route("{id}")]
    public IActionResult AnyV2OrHigherWithId()
    {
        return _generator.Generate();
    }

    [HttpDelete("{id}")]
    public IActionResult Delete()
    {
        return _generator.Generate();
    }
}
