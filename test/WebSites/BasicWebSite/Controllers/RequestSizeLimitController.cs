// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BasicWebSite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BasicWebSite.Controllers
{
    [RequestSizeLimit(500)]
    public class RequestSizeLimitController : Controller
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RequestSizeLimitCheckBeforeAntiforgeryValidation(Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return Json(product);
        }

        [HttpPost]
        [DisableRequestSizeLimit]
        public IActionResult DisableRequestSizeLimit([FromBody] Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            return Json(product);
        }
    }
}
