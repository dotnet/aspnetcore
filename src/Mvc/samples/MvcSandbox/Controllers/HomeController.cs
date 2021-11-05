// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace MvcSandbox.Controllers;

public class HomeController : Controller
{
    [ModelBinder]
    public string Id { get; set; }

    public IActionResult Index()
    {
        return View();
    }
}
