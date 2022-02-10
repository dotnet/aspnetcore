// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace ApplicationModelWebSite.Controllers;

[MultipleAreas("Products", "Services", "Manage")]
public class MultipleAreasController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
