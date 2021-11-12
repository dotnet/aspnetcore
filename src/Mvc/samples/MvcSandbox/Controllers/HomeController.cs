// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace MvcSandbox.Controllers;

public class HomeController : Controller
{
    [ModelBinder]
    public string Id { get; set; }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public IActionResult Index(Person person)
    {
        return View();
    }
}

public class Person
{
    public string Name { get; set; }

    public int Age { get; set; }
}
