// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers
{
    [RequestFormLimits(ValueCountLimit = 2)]
    public class RequestFormLimitsController : Controller
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RequestFormLimitsBeforeAntiforgeryValidation(Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return Json(product);
        }

        [HttpPost]
        [RequestFormLimits(ValueCountLimit = 5)]
        public IActionResult OverrideControllerLevelLimits(Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            return Json(product);
        }

        [HttpPost]
        [RequestFormLimits]
        public IActionResult OverrideControllerLevelLimitsUsingDefaultLimits(Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            return Json(product);
        }

        [HttpPost]
        [RequestFormLimits(ValueCountLimit = 2)]
        [RequestSizeLimit(100)]
        [ValidateAntiForgeryToken]
        public IActionResult RequestSizeLimitBeforeRequestFormLimits(Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            return Json(product);
        }
    }
}
