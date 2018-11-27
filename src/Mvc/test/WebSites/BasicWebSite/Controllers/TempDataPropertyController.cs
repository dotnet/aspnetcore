// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicWebSite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Net;

namespace BasicWebSite.Controllers
{
    public class TempDataPropertyController : Controller
    {
        [TempData]
        public string Message { get; set; }

        [TempData]
        public int? NullableInt { get; set; }

        [HttpPost]
        public IActionResult CreateForView(Person person)
        {
            Message = "Success (from Temp Data)";
            NullableInt = 100;
            return RedirectToAction("DetailsView", person);
        }

        [HttpPost]
        public IActionResult Create(Person person)
        {
            Message = "Success (from Temp Data)";
            NullableInt = 100;
            return RedirectToAction("Details", person);
        }

        public IActionResult DetailsView(Person person)
        {
            ViewData["Message"] = Message;
            ViewData["NullableInt"] = NullableInt;
            return View(person);
        }

        public string Details(Person person)
        {
            return $"{Message}{NullableInt} for person {person.FullName} with id {person.id}.";
        }

        public StatusCodeResult CreateNoRedirect(Person person)
        {
            Message = "Success (from Temp Data)";
            NullableInt = 100;
            return new OkResult();
        }

        public string TempDataKept()
        {
            TempData.Keep();
            return Message + NullableInt;
        }

        public string ReadTempData()
        {
            return Message + NullableInt;
        }
    }
}
