// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace MvcSandbox.Controllers;

[Route("[controller]/[action]")]
public class LoginController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
