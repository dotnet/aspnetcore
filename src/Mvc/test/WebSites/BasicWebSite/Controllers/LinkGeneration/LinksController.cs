// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers.LinkGeneration
{
    public class LinksController : Controller
    {
        public IActionResult Index(string view)
        {
            return View(viewName: view);
        }

        public string Details()
        {
            throw new NotImplementedException();
        }
    }
}
