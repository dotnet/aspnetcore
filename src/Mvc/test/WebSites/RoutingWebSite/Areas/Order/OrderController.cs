// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite.Areas.Order;

[Area("Order")]
[Route("Order/[action]", Name = "[area]_[action]")]
public class OrderController : Controller
{
    private readonly TestResponseGenerator _generator;

    public OrderController(TestResponseGenerator generator)
    {
        _generator = generator;
    }

    [HttpGet]
    public IActionResult GetOrder()
    {
        return _generator.Generate("/Order/GetOrder");
    }
}
