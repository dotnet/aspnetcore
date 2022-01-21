// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite;

// This controller is reachable via traditional routing.
[Area("Travel")]
public class FlightController
{
    private readonly TestResponseGenerator _generator;

    public FlightController(TestResponseGenerator generator)
    {
        _generator = generator;
    }

    public IActionResult Index()
    {
        return _generator.Generate("/Travel/Flight", "/Travel/Flight/Index");
    }

    [HttpPost]
    public IActionResult BuyTickets()
    {
        return _generator.Generate("/Travel/Flight/BuyTickets");
    }
}
