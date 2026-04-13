// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using MvcDemo.Models;

namespace MvcDemo.Controllers;

public class RegistrationController : Controller
{
    public IActionResult Index()
    {
        return View(new RegistrationModel());
    }

    [HttpPost]
    public IActionResult Index(RegistrationModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        ViewData["Success"] = true;
        return View(new RegistrationModel());
    }
}
