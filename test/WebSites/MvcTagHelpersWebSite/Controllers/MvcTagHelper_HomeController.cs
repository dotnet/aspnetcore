// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using MvcTagHelpersWebSite.Models;

namespace MvcTagHelpersWebSite.Controllers
{
    public class MvcTagHelper_HomeController : Controller
    {
        public IActionResult Order()
        {
            var products = new List<Product>();
            products = new List<Product>();

            for (int i = 7; i < 13; ++i)
            {
                products.Add(new Product()
                {
                    ProductName = "Product_" + i,
                    Number = i,
                    PartNumbers = Enumerable.Range(1, 3).Select(n => string.Format("{0}-{1}", i, n))
                });
            }

            ViewBag.Items = new SelectList(products, "Number", "ProductName", 9);

            var order = new Order()
            {
                Shipping = "UPSP",
                Customer = new Customer()
                {
                    Key = "KeyA",
                    Number = 1,
                    Gender = Gender.Female,
                    Name = "NameStringValue",
                },
                NeedSpecialHandle = true,
                PaymentMethod = new List<string> { "Check" }
            };

            return View(order);
        }

        public IActionResult Product()
        {
            var product = new Product()
            {
                HomePage = new System.Uri("http://www.contoso.com"),
                Description = "Type the product description"
            };
            return View(product);
        }

        public IActionResult ProductSubmit(Product product)
        {
            throw new NotImplementedException();
        }

        public IActionResult Customer()
        {
            return View();
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
