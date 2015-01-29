// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc;
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

        public IActionResult Help()
        {
            return View();
        }

        public ViewResult NestedViewStartTagHelper()
        {
            return View();
        }

        public ViewResult ViewWithLayoutAndNestedTagHelper()
        {
            return View();
        }

        public ViewResult ViewWithInheritedRemoveTagHelper()
        {
            return View("/Views/RemoveTagHelperViewStart/ViewWithInheritedRemoveTagHelper.cshtml");
        }
    }
}