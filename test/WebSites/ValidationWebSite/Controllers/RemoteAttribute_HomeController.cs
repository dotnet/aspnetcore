// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using ValidationWebSite.Models;

namespace ValidationWebSite.Controllers
{
    public class RemoteAttribute_HomeController : Controller
    {
        private static Person _person;

        [HttpGet]
        [Route("[Controller]/[Action]")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Route("[Controller]/[Action]")]
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

        [Route("", Name = "Home", Order = -1)]
        [Route("[Controller]/Index")]
        public IActionResult Details()
        {
            return View(_person);
        }
    }
}