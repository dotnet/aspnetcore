// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite
{
    // This controller contains only actions with individual attribute routes.
    public class StoreController : Controller
    {
        private readonly TestResponseGenerator _generator;

        public StoreController(TestResponseGenerator generator)
        {
            _generator = generator;
        }

        [HttpGet("Store/Shop/Products")]
        public IActionResult ListProducts()
        {
            return _generator.Generate("/Store/Shop/Products");
        }

        // Intentionally designed to conflict with HomeController#About.
        [HttpGet("Home/About")]
        public IActionResult About()
        {
            return _generator.Generate("/Home/About");
        }

        [Route("Store/Shop/Orders")]
        public IActionResult Orders()
        {
            return _generator.Generate("/Store/Shop/Orders");
        }

        [HttpGet("Store/Shop/Orders")]
        public IActionResult GetOrders()
        {
            return _generator.Generate("/Store/Shop/Orders");
        }
    }
}