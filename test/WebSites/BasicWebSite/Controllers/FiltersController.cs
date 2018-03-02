// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using BasicWebSite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers
{
    public class FiltersController : Controller
    {
        [HttpPost]
        [Consumes("application/yaml")]
        [UnprocessableResultFilter]
        public IActionResult AlwaysRunResultFiltersCanRunWhenResourceFilterShortCircuit([FromBody] Product product) =>
            throw new Exception("Shouldn't be executed");

        [ServiceFilter(typeof(ServiceActionFilter))]
        public IActionResult ServiceFilterTest() => Content("Service filter content");

        [TraceResultOutputFilter]
        public IActionResult TraceResult() => new EmptyResult();

        [Route("{culture}/[controller]/[action]")]
        [MiddlewareFilter(typeof(LocalizationPipeline))]
        public IActionResult MiddlewareFilterTest()
        {
            return Content($"CurrentCulture:{CultureInfo.CurrentCulture.Name},CurrentUICulture:{CultureInfo.CurrentUICulture.Name}");
        }
    }
}
