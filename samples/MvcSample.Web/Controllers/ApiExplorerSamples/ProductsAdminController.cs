// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
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
        public void AddProduct([FromBody] Product product)
        {
        }

        [HttpPost("{id}")]
        public void UpdateProduct([FromBody] Product product)
        {
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