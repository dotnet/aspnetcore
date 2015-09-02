// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using ActivatorWebSite.Models;
using Microsoft.AspNet.Mvc;

namespace ActivatorWebSite
{
    /// <summary>
    /// Controller that verifies if view activation works.
    /// </summary>
    public class ViewController : Controller
    {
        public IActionResult ConsumeDefaultProperties()
        {
            return View();
        }

        public IActionResult ConsumeInjectedService()
        {
            return View();
        }

        public IActionResult ConsumeServicesFromBaseType()
        {
            return View();
        }

        public IActionResult ConsumeViewComponent()
        {
            return View();
        }

        public IActionResult ConsumeCannotBeActivatedComponent()
        {
            return View();
        }

        public IActionResult UseTagHelper()
        {
            var item = new Item
            {
                Name = "Fake"
            };
            return View(item);
        }
    }
}