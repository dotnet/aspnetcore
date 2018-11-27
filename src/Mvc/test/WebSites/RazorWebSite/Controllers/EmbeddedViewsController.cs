// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace RazorWebSite.Controllers
{
    public class EmbeddedViewsController : Controller
    {
        public IActionResult LookupByName() => View("Index");

        public IActionResult LookupByPath() => View("/Views/EmbeddedViews/Index.cshtml");

        public IActionResult RelativeNonPath() => View();
    }
}
