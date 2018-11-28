// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace SecurityWebSite.Controllers
{
    [IgnoreAntiforgeryToken]
    public class IgnoreAntiforgeryController : Controller
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index()
        {
            return Content("Ok");
        }
    }
}
