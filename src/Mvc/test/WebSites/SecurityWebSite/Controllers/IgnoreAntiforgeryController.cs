// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace SecurityWebSite.Controllers;

[IgnoreAntiforgeryToken]
public class IgnoreAntiforgeryController : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index()
    {
        return Content("Ok");
    }
}
