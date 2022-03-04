// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace ControllersFromServicesWebSite
{
    [Route("/[controller]")]
    public class AnotherController : Controller
    {
        [HttpGet]
        public IActionResult Get()
        {
            return new ContentResult { Content = "1" };
        }

        [HttpGet("InServicesViewComponent")]
        public IActionResult ViewComponentAction()
        {
            return ViewComponent("ComponentFromServices");
        }

        [HttpGet("InServicesTagHelper")]
        public IActionResult InServicesTagHelper()
        {
            return View();
        }
    }
}
