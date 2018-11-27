// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace RazorWebSite.Controllers
{
    // Views returned by this controller use #ifdefs for defines specified in the project
    // The intent of this controller is to verify that view compilation uses the app's compilation settings.
    public class ViewsConsumingCompilationOptionsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}