// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers;

public class NonNullableController : Controller
{
    public ActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public ActionResult Index(NonNullablePerson person, string description)
    {
        if (ModelState.IsValid)
        {
            return RedirectToAction();
        }

        return View(person);
    }

    public class NonNullablePerson
    {
        public string Name { get; set; } = default!;
    }
}
