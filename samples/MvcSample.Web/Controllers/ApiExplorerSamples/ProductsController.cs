// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc;

namespace MvcSample.Web.ApiExplorerSamples
{
    [ApiExplorerSettings(GroupName = "Public API")]
    [Produces("application/json")]
    [Route("api/Products")]
    public class ProductsController : Controller
    {
        [HttpGet("{id:int}")]
        public Product GetById(int id)
        {
            return null;
        }

        [HttpGet("Search/{name}")]
        public IEnumerable<Product> SearchByName(string name)
        {
            return null;
        }

        [Produces("application/json", Type = typeof(ProductOrderConfirmation))]
        [HttpPut("{id:int}/Buy")]
        public IActionResult Buy(int projectId, int quantity = 1)
        {
            return null;
        }

        [Produces("application/json", Type = typeof(ProductOrderConfirmation))]
        [HttpPut("{order.acountId:int}/PlaceOrder")]
        public IActionResult PlaceOrder(Order order)
        {
            return null;
        }
    }
}