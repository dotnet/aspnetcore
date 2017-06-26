// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using HtmlGenerationWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace HtmlGenerationWebSite.Controllers
{
    public class HtmlGeneration_WeirdExpressionsController : Controller
    {
        public IActionResult GetWeirdWithHtmlHelpers()
        {
            return View(new WeirdModel());
        }

        public IActionResult GetWeirdWithTagHelpers()
        {
            return View(new WeirdModel());
        }
    }
}