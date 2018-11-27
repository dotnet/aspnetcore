// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using HtmlGenerationWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace HtmlGenerationWebSite.Controllers
{
    public class CheckViewData : Controller
    {
        public IActionResult AtViewModel()
        {
            return View(new SuperViewModel());
        }

        public IActionResult NullViewModel()
        {
            return View("AtViewModel");
        }

        public IActionResult ViewModel()
        {
            return View(new SuperViewModel());
        }
    }
}
