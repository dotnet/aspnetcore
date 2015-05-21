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
        public ViewResult ConsumeDefaultProperties()
        {
            return View();
        }

        public ViewResult ConsumeInjectedService()
        {
            return View();
        }

        public ViewResult ConsumeServicesFromBaseType()
        {
            return View();
        }

        public ViewResult ConsumeViewComponent()
        {
            return View();
        }

        public ViewResult ConsumeCannotBeActivatedComponent()
        {
            return View();
        }

        public ViewResult UseTagHelper()
        {
            var item = new Item
            {
                Name = "Fake"
            };
            return View(item);
        }
    }
}