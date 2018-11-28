// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace ControllersFromServicesWebSite
{
    public class NotInServicesController : Controller
    {
        [HttpGet("/not-discovered/not-in-services")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
