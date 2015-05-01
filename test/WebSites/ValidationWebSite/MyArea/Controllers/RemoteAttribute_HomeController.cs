// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using ValidationWebSite.Models;

namespace ValidationWebSite.MyArea.Controllers
{
    [Area("Aria")]
    [Route("[Area]/[Controller]/[Action]", Order = -2)]
    public class RemoteAttribute_HomeController : Controller
    {
        private static Person _person;

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Person person)
        {
            ModelState.Remove("id");
            if (!ModelState.IsValid)
            {
                return View(person);
            }

            _person = person;
            return RedirectToAction(nameof(Details));
        }

        [Route("/[Area]", Name = "AriaHome", Order = -3)]
        [Route("/[Area]/[Controller]/Index", Order = -2)]
        public IActionResult Details()
        {
            return View(_person);
        }
    }
}