// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite;

[Route("Friends")]
public class FriendsController : Controller
{
    private readonly TestResponseGenerator _generator;

    public FriendsController(TestResponseGenerator generator)
    {
        _generator = generator;
    }

    [HttpGet]
    [HttpGet("{id}")]
    public IActionResult Get([FromRoute] string id)
    {
        return _generator.Generate(id == null ? "/Friends" : $"/Friends/{id}");
    }

    [HttpDelete]
    public IActionResult Delete()
    {
        return _generator.Generate("/Friends");
    }
}
