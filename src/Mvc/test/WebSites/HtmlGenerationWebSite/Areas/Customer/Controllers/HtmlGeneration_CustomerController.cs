// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace HtmlGenerationWebSite.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HtmlGeneration_CustomerController : Controller
    {
        public IActionResult Index(Models.Customer customer)
        {
            return View("Customer");
        }
    }
}