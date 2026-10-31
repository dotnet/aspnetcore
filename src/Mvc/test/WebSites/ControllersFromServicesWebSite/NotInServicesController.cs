// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace ControllersFromServicesWebSite;

public class NotInServicesController : Controller
{
    [HttpGet("/not-discovered/not-in-services")]
    public IActionResult Index()
    {
        return View();
    }
}
