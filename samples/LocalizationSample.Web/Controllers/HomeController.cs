// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using LocalizationSample.Web.Models;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Localization;

namespace LocalizationSample.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHtmlLocalizer _localizer;

        public HomeController(IHtmlLocalizer<HomeController> localizer)
        {
            _localizer = localizer;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Locpage()
        {
            ViewData["Message"] = _localizer["Learn More"];
            return View();
        }

        public IActionResult GetInvalidUser()
        {
            var user = new User
            {
                Name = "A",
                Product = new Product()
            };

            TryValidateModel(user);
            return View(user);
        }
    }
}
