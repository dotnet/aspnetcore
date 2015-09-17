// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc;

namespace ActionResultsWebSite
{
    public class HomeController : Controller
    {
        public IActionResult Index([FromBody] DummyClass test)
        {
            if (!ModelState.IsValid)
            {
                return HttpBadRequest(ModelState);
            }

            return Content("Hello World!");
        }

        public IActionResult GetCustomErrorObject([FromBody] DummyClass test)
        {
            if (!ModelState.IsValid)
            {
                var errors = new List<string>();
                errors.Add("Something went wrong with the model.");
                return new BadRequestObjectResult(errors);
            }

            return Content("Hello World!");
        }
    }
}