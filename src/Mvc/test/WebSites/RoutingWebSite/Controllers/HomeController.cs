// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite;

// This controller is reachable via traditional routing.
public class HomeController : Controller
{
    private readonly TestResponseGenerator _generator;

    public HomeController(TestResponseGenerator generator)
    {
        _generator = generator;
    }

    public IActionResult Index()
    {
        return _generator.Generate("/", "/Home", "/Home/Index");
    }

    public IActionResult About()
    {
        // There are no urls that reach this action - it's hidden by an attribute route.
        return _generator.Generate();
    }

    public IActionResult Contact()
    {
        return _generator.Generate("/Home/Contact");
    }

    public IActionResult OptionalPath(string path = "default")
    {
        return _generator.Generate("/Home/OptionalPath/" + path);
    }
}
