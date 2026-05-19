// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RazorWebSite;

public class TagHelperComponentController : Controller
{
    // GET: /<controller>/
    public IActionResult GetHead()
    {
        return View("Head");
    }

    public IActionResult GetBody()
    {
        return View("Body");
    }
}
