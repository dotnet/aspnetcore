// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;
using TagHelpersWebSite.Models;

namespace TagHelpersWebSite.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index(bool approved = false)
        {
            return View(new WebsiteContext
            {
                Approved = approved,
                CopyrightYear = 2015,
                Version = new Version(1, 3, 3, 7),
                TagsToShow = 20
            });
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult GlobbingTagHelpers()
        {
            return View();
        }

        public IActionResult Help()
        {
            return View();
        }

        public IActionResult MyHtml()
        {
            return View();
        }

        public IActionResult ViewComponentTagHelpers()
        {
            return View();
        }

        public IActionResult UnboundDynamicAttributes()
        {
            return View();
        }

        public IActionResult NestedViewImportsTagHelper()
        {
            return View();
        }

        public IActionResult ViewWithLayoutAndNestedTagHelper()
        {
            return View();
        }

        public IActionResult ViewWithInheritedRemoveTagHelper()
        {
            return View("/Views/RemoveInheritedTagHelpers/ViewWithInheritedRemoveTagHelper.cshtml");
        }

        public IActionResult ViewWithInheritedTagHelperPrefix()
        {
            return View("/Views/InheritedTagHelperPrefix/InheritedTagHelperPrefix.cshtml");
        }

        public IActionResult ViewWithOverriddenTagHelperPrefix()
        {
            return View("/Views/InheritedTagHelperPrefix/OverriddenTagHelperPrefix.cshtml");
        }

        public IActionResult ViewWithNestedInheritedTagHelperPrefix()
        {
            return View(
                "/Views/InheritedTagHelperPrefix/NestedInheritedTagHelperPrefix/" +
                "NestedInheritedTagHelperPrefix.cshtml");
        }

        public IActionResult ViewWithNestedOverriddenTagHelperPrefix()
        {
            return View(
                "/Views/InheritedTagHelperPrefix/NestedInheritedTagHelperPrefix/" +
                "NestedOverriddenTagHelperPrefix.cshtml");
        }
    }
}