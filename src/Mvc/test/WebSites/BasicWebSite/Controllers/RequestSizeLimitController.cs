// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers
{
    [RequestSizeLimit(500)]
    public class RequestSizeLimitController : Controller
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult<Product> RequestSizeLimitCheckBeforeAntiforgeryValidation(Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return product;
        }

        [HttpPost]
        [DisableRequestSizeLimit]
        public ActionResult<Product> DisableRequestSizeLimit([FromBody] Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            return product;
        }
    }
}
