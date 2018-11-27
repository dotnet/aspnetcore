// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using BasicWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers
{
    public class ActionResultOfTController : Controller
    {
        [HttpGet]
        public ActionResult<Product> GetProduct(int? productId)
        {
            if (productId == null)
            {
                return BadRequest();
            }

            return new Product { SampleInt = productId.Value, };
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsAsync()
        {
            await Task.Delay(0);
            return new[] { new Product(), new Product() };
        }
    }
}
