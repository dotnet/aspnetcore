// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace RoutingWebSite.Products.US
{
    [CountrySpecific("US")]
    public class ProductsController : Controller
    {
        private readonly TestResponseGenerator _generator;

        public ProductsController(TestResponseGenerator generator)
        {
            _generator = generator;
        }

        public IActionResult GetProducts()
        {
            return _generator.Generate("/api/Products/US/GetProducts");
        }
    }
}