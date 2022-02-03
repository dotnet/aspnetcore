// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite;

[Route("api/v1/Maps", Name = "v1", Order = 1)]
[Route("api/v2/Maps")]
public class MapsController : Controller
{
    private readonly TestResponseGenerator _generator;

    public MapsController(TestResponseGenerator generator)
    {
        _generator = generator;
    }

    [HttpGet]
    public ActionResult Get()
    {
        // Multiple attribute routes with name and order.
        // We will always generate v2 routes except when
        // we explicitly use "v1" to generate a v1 route.
        return _generator.Generate(
            Url.Action(),
            Url.RouteUrl("v1"),
            Url.RouteUrl(new { }));
    }

    [HttpPost("/api/v2/Maps")]
    public ActionResult Post()
    {
        return _generator.Generate(
            Url.Action(),
            Url.RouteUrl(new { }));
    }

    [HttpPut("{id}")]
    [HttpPatch("PartialUpdate/{id}")]
    public ActionResult Update(int id)
    {
        // We will generate "/api/v2/Maps/PartialUpdate/{id}"
        // in both cases, v1 routes will be discarded due to their
        // Order and for v2 routes PartialUpdate has higher precedence.
        // api/v1/Maps/{id} and api/v2/Maps/{id} will only match on PUT.
        // api/v1/Maps/PartialUpdate/{id} and api/v2/Maps/PartialUpdate/{id} will only match on PATCH.
        return _generator.Generate(
            Url.Action(),
            Url.RouteUrl(new { }));
    }
}
