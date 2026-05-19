// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite;

// This controller contains only actions with individual attribute routes.
public class StoreController : Controller
{
    private readonly TestResponseGenerator _generator;

    public StoreController(TestResponseGenerator generator)
    {
        _generator = generator;
    }

    [HttpGet("Store/Shop/Products")]
    public IActionResult ListProducts()
    {
        return _generator.Generate("/Store/Shop/Products");
    }

    // Intentionally designed to conflict with HomeController#About.
    [HttpGet("Home/About")]
    public IActionResult About()
    {
        return _generator.Generate("/Home/About");
    }

    [Route("Store/Shop/Orders")]
    public IActionResult Orders()
    {
        return _generator.Generate("/Store/Shop/Orders");
    }

    [HttpGet("Store/Shop/Orders")]
    public IActionResult GetOrders()
    {
        return _generator.Generate("/Store/Shop/Orders");
    }
}
