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
        public ActionResult<Product> RequestFormLimitsBeforeAntiforgeryValidation(Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return product;
        }

        [HttpPost]
        [RequestFormLimits(ValueCountLimit = 5)]
        public ActionResult<Product> OverrideControllerLevelLimits(Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            return product;
        }

        [HttpPost]
        [RequestFormLimits]
        public ActionResult<Product> OverrideControllerLevelLimitsUsingDefaultLimits(Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            return product;
        }

        [HttpPost]
        [RequestFormLimits(ValueCountLimit = 2)]
        [RequestSizeLimit(100)]
        [ValidateAntiForgeryToken]
        public ActionResult<Product> RequestSizeLimitBeforeRequestFormLimits(Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            return product;
        }
    }
}
