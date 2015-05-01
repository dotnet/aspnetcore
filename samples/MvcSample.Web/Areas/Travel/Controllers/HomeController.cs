// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace MvcSample.Web.Areas.Travel.Controllers
{
    [Area("Travel")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return Content("This is the Travel/Home/Index action.");
        }
    }
}