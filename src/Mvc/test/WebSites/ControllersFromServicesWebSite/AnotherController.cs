// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace ControllersFromServicesWebSite;

[Route("/[controller]")]
public class AnotherController : Controller
{
    [HttpGet]
    public IActionResult Get()
    {
        return new ContentResult { Content = "1" };
    }

    [HttpGet("InServicesViewComponent")]
    public IActionResult ViewComponentAction()
    {
        return ViewComponent("ComponentFromServices");
    }

    [HttpGet("InServicesTagHelper")]
    public IActionResult InServicesTagHelper()
    {
        return View();
    }
}
