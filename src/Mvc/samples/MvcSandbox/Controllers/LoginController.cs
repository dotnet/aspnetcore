// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace MvcSandbox.Controllers
{
    [Route("[controller]/[action]")]
    public class LoginController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }

    [ApiController]
    [Route("[controller]")]
    public class WeatherController : ControllerBase
    {
        [HttpGet]
        public string Get() => "Hello Sunny Days!";
    }
}
