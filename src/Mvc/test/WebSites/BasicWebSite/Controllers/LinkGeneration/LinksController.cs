// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers.LinkGeneration;

public class LinksController : Controller
{
    public IActionResult Index(string view)
    {
        return View(viewName: view);
    }

    public string Details()
    {
        throw new NotImplementedException();
    }
}
