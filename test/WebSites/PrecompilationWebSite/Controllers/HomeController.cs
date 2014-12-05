// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace PrecompilationWebSite.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult PrecompiledViewsCanConsumeCompilationOptions()
        {
            return View("~/Views/ViewsConsumingCompilationOptions/Index");
        }
    }
}
