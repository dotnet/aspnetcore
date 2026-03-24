// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using MvcDemo.Models;

namespace MvcDemo.Controllers;

public class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Contact()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Contact(ContactModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        TempData["Success"] = "Your message has been sent successfully!";

        return RedirectToAction(nameof(Contact));
    }
}
