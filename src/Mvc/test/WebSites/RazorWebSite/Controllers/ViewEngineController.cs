// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RazorWebSite.Controllers;

public class ViewEngineController : Controller
{
    public IActionResult ViewWithoutLayout()
    {
        return View();
    }

    public IActionResult ViewWithFullPath()
    {
        return View("/Views/ViewEngine/ViewWithFullPath.cshtml");
    }

    public IActionResult ViewWithRelativePath()
    {
        return View("Views/ViewEngine/ViewWithRelativePath.cshtml");
    }

    public IActionResult ViewWithLayout()
    {
        return View();
    }

    public IActionResult ViewWithNestedLayout()
    {
        return View();
    }

    public IActionResult ViewWithPartial()
    {
        ViewData["TestKey"] = "test-value";
        var model = new Person
        {
            Address = new Address { ZipCode = "98052" }
        };

        return View(model);
    }

    public IActionResult ViewWithPartialTakingModelFromIEnumerable()
    {
        var model = new List<Person>()
            {
                new Person() { Name = "Hello" },
                new Person() { Name = "World" }
            };

        return View(model);
    }

    public IActionResult ViewPassesViewDataToLayout()
    {
        ViewData["Title"] = "Controller title";
        return View("ViewWithTitle");
    }

    public IActionResult ViewWithDataFromController()
    {
        ViewData["data-from-controller"] = "hello from controller";
        return View("ViewWithDataFromController");
    }

    public IActionResult ViewWithComponentThatHasLayout()
    {
        ViewData["Title"] = "View With Component With Layout";
        return View();
    }

    public IActionResult ViewWithComponentThatHasViewStart()
    {
        return View();
    }

    public IActionResult SearchInPages() => View();
}
