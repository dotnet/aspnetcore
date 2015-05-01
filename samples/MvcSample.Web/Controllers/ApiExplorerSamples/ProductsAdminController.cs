// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc;

namespace MvcSample.Web.ApiExplorerSamples
{
    [ApiExplorerSettings(GroupName = "Admin API")]
    [Route("api/Admin/Products")]
    public class ProductsAdminController : Controller
    {
        [HttpPut]
        [Produces("application/json", Type = typeof(Product))]
        public IActionResult AddProduct([FromBody] Product product)
        {
            return null;
        }

        [HttpPost("{id?}")]
        [Produces("application/json", Type = typeof(Product))]
        public IActionResult UpdateProduct(UpdateProductDTO dto)
        {
            return null;
        }

        [HttpPost("{id}/Stock")]
        public void SetQuantityInStock(int id, int quantity)
        {
        }

        [HttpPost("{id}/Price")]
        public void SetPrice(int id, decimal price)
        {
        }

        [Produces("application/json", "application/xml")]
        [HttpGet("{id}/Orders")]
        public IEnumerable<ProductOrderConfirmation> GetOrders(DateTime? fromData = null, DateTime? toDate = null)
        {
            return null;
        }
    }
}