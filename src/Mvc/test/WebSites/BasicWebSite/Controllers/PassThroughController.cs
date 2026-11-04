// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicWebSite.Components;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers;

public class PassThroughController : Controller
{
    public IActionResult Index(long value)
    {
        return ViewComponent(typeof(PassThroughViewComponent), new { value });
    }
}
