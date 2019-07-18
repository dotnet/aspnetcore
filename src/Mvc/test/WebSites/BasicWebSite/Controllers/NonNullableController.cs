// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers
{
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
}
