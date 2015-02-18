// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.JsonPatch;
using Microsoft.AspNet.Mvc;

namespace MvcSample.Web.Controllers
{
    [Route("api/[controller]")]
    public class JsonPatchController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        [HttpPatch]
        public IActionResult Patch([FromBody] JsonPatchDocument<Customer> patchDoc)
        {
            var customer = new Customer
            {
                Name = "John",
                Orders = new List<Order>()
                {
                    new Order
                    {
                        OrderName = "Order1"
                    },
                    new Order
                    {
                        OrderName = "Order2"
                    }
                }
            };

            patchDoc.ApplyTo(customer);

            return new ObjectResult(customer);
        }

        public class Customer
        {
            public string Name { get; set; }

            public List<Order> Orders { get; set; }
        }

        public class Order
        {
            public string OrderName { get; set; }
        }
    }
}
