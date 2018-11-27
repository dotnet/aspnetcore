// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace RazorWebSite
{
    public class TagHelperComponentController : Controller
    {
        // GET: /<controller>/
        public IActionResult GetHead()
        {
            return View("Head");
        }

        public IActionResult GetBody()
        {
            return View("Body");
        }
    }
}
