// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;

namespace RazorWebSite.Controllers
{
    public class HtmlHelperOptionsController : Controller
    {
        public IActionResult HtmlHelperOptionsDefaultsInView()
        {
            var model = new DateModel
            {
                MyDate = new DateTimeOffset(
                year: 2000,
                month: 1,
                day: 2,
                hour: 3,
                minute: 4,
                second: 5,
                millisecond: 60,
                offset: TimeSpan.FromHours(0))
            };

            ModelState.AddModelError(string.Empty, "A model error occurred.");
            ModelState.AddModelError("Error", "An error occurred.");
            return View(model);
        }

        public IActionResult OverrideAppWideDefaultsInView()
        {
            var model = new DateModel
            {
                MyDate = new DateTimeOffset(
                year: 2000,
                month: 1,
                day: 2,
                hour: 3,
                minute: 4,
                second: 5,
                millisecond: 60,
                offset: TimeSpan.FromHours(0))
            };

            ModelState.AddModelError(string.Empty, "A model error occurred.");
            ModelState.AddModelError("Error", "An error occurred.");
            return View(model);
        }
    }

    public class DateModel
    {
        public DateTimeOffset MyDate { get; set; }
    }
}
