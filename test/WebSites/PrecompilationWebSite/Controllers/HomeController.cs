// Copyright (c) .NET Foundation. All rights reserved.
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
            return View("~/Views/ViewsConsumingCompilationOptions/Index.cshtml");
        }

        public IActionResult GlobalDeletedPriorToFirstRequest()
        {
            return View("~/Views/ViewImportsDelete/Index.cshtml");
        }

        [HttpGet("/Test")]
        public IActionResult TestView()
        {
            return View("~/Views/Test/Index.cshtml");
        }
    }
}
