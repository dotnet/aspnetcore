// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers;

[Route("[controller]/[action]")]
public class RemoteAttribute_HomeController : Controller
{
    private static RemoteAttributeUser _user;

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(RemoteAttributeUser user)
    {
        if (!ModelState.IsValid)
        {
            return View(user);
        }

        _user = user;
        return RedirectToAction(nameof(Details));
    }

    public IActionResult Details()
    {
        return View(_user);
    }
}
