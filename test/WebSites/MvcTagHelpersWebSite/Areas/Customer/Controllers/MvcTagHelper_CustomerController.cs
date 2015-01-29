// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;

namespace MvcTagHelpersWebSite.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class MvcTagHelper_CustomerController : Controller
    {
        public IActionResult Index(MvcTagHelpersWebSite.Models.Customer customer)
        {
            return View("Customer");
        }
    }
}