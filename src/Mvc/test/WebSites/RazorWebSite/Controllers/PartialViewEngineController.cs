// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace RazorWebSite.Controllers
{
    public class PartialViewEngineController : Controller
    {
        public IActionResult ViewWithoutLayout()
        {
            return PartialView();
        }

        public IActionResult ViewWithFullPath()
        {
            return PartialView("/Views/ViewEngine/ViewWithFullPath.cshtml");
        }

        public IActionResult PartialViewWithNamePassedIn()
        {
            return PartialView("ViewWithLayout");
        }

        public IActionResult ViewWithNestedLayout()
        {
            return PartialView();
        }

        public IActionResult PartialWithDataFromController()
        {
            ViewData["data-from-controller"] = "hello from controller";
            return PartialView("ViewWithDataFromController");
        }

        public IActionResult PartialWithModel()
        {
            var model = new Person
            {
                Name = "my name is judge",
                Address = new Address { ZipCode = "98052" }
            };
            return PartialView(model);
        }

        public IActionResult ViewPartialMissingSection()
        {
            return View();
        }
    }
}