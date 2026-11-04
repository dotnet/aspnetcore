// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite;

[Route("/{controller:slugify}")]
public class EndpointRoutingController : Controller
{
    private readonly TestResponseGenerator _generator;

    public EndpointRoutingController(TestResponseGenerator generator)
    {
        _generator = generator;
    }

    [Route("/{controller}/{action=Index}")]
    public IActionResult Index()
    {
        return _generator.Generate("/EndpointRouting/Index", "/EndpointRouting");
    }

    [Route("/{controller:slugify}/{action}")]
    public IActionResult ParameterTransformer()
    {
        return _generator.Generate("/endpoint-routing/ParameterTransformer");
    }

    [Route("{id}")]
    public IActionResult Get(int id)
    {
        return _generator.Generate("/endpoint-routing/" + id);
    }
}
