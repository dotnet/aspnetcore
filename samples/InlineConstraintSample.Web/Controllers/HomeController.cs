// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace InlineConstraintSample.Web.Controllers
{
    public class HomeController : Controller
    {
        [Route("")]
        [Route("[controller]")]
        [Route("[controller]/[action]")]
        public IActionResult Index()
        {
            return View();
        }
    }
}