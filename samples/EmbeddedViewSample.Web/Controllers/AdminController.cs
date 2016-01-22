// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace EmbeddedViewSample.Web.Controllers
{
    [Area("Restricted")]
    public class AdminController : Controller
    {
        public IActionResult Login()
        {
            return View();
        }
    }
}