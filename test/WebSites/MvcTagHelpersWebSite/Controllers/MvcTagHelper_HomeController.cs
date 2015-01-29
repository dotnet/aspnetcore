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
        private readonly List<Product> _products = new List<Product>();

        public MvcTagHelper_HomeController()
        {
            _products.Add(new Product
            {
                ProductName = "Product_0",
                Number = 0,
                HomePage = new Uri("http://www.contoso.com")
            });
            _products.Add(new Product
            {
                ProductName = "Product_1",
                Number = 1,
            });
            _products.Add(new Product
            {
                ProductName = "Product_2",
                Number = 2,
                Description = "Product_2 desription"
            });
        }

        public IActionResult Order()
        {
            ViewBag.Items = new SelectList(_products, "Number", "ProductName", 2);

            var order = new Order
            {
                Shipping = "UPSP",
                Customer = new Customer
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
            var product = new Product
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

        public IActionResult ProductList()
        {
            return View(_products);
        }

        public IActionResult EmployeeList()
        {
            var employees = new List<Employee>();

            employees.Add(new Employee
            {
                Name = "EmployeeName_0",
                Number = 0,
                Address = "Employee_0 address"
            });
            employees.Add(new Employee
            {
                Name = "EmployeeName_1",
                Number = 1,
                OfficeNumber = "1002",
                Gender = Gender.Female
            });
            employees.Add(new Employee
            {
                Name = "EmployeeName_2",
                Number = 2,
                Remote = true
            });

            return View(employees);
        }

        public IActionResult CreateWarehouse()
        {
            ViewBag.Items = new SelectList(_products, "Number", "ProductName", 9);

            return View();
        }

        public IActionResult EditWarehouse()
        {
            var warehouse = new Warehouse
            {
                City = "City_1",
                Employee = new Employee
                {
                    Name = "EmployeeName_1",
                    Number = 1,
                    Address = "Address_1",
                    PhoneNumber = "PhoneNumber_1",
                    Gender = Gender.Female
                }
            };
            return View(warehouse);
        }
    }
}
