// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RazorWebSite.Controllers;

public class UrlResolutionController : Controller
{
    public IActionResult Index()
    {
        var model = new Person
        {
            Name = "John Doe"
        };

        return View(model);
    }
}
