// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace RazorWebSite.Controllers
{
    public class ViewNameSpecification_HomeController : Controller
    {
        public IActionResult LayoutSpecifiedWithPartialPathInViewStart()
        {
            return View();
        }

        public IActionResult LayoutSpecifiedWithPartialPathInViewStart_ForViewSpecifiedWithPartialName()
        {
            return View("LayoutSpecifiedWithPartialPathInViewStart");
        }

        public IActionResult LayoutSpecifiedWithPartialPathInViewStart_ForViewSpecifiedWithAppRelativePath()
        {
            return View("~/Views/ViewNameSpecification_Home/LayoutSpecifiedWithPartialPathInViewStart");
        }

        public IActionResult LayoutSpecifiedWithPartialPathInViewStart_ForViewSpecifiedWithAppRelativePathWithExtension()
        {
            return View("~/Views/ViewNameSpecification_Home/LayoutSpecifiedWithPartialPathInViewStart.cshtml");
        }

        public IActionResult LayoutSpecifiedWithPartialPathInPage()
        {
            return View();
        }

        public IActionResult LayoutSpecifiedWithPartialPathInPageWithPartialPath()
        {
            return View("LayoutSpecifiedWithPartialPathInPage");
        }

        public IActionResult LayoutSpecifiedWithPartialPathInPageWithAppRelativePath()
        {
            return View("~/Views/ViewNameSpecification_Home/LayoutSpecifiedWithPartialPathInPage");
        }

        public IActionResult LayoutSpecifiedWithPartialPathInPageWithAppRelativePathWithExtension()
        {
            return View("~/Views/ViewNameSpecification_Home/LayoutSpecifiedWithPartialPathInPage.cshtml");
        }

        public IActionResult LayoutSpecifiedWithNonPartialPath()
        {
            ViewData["Layout"] = "~/Views/ViewNameSpecification_Home/_NonSharedLayout";
            return View("PageWithNonPartialLayoutPath");
        }

        public IActionResult LayoutSpecifiedWithNonPartialPathWithExtension()
        {
            ViewData["Layout"] = "~/Views/ViewNameSpecification_Home/_NonSharedLayout.cshtml";
            return View("PageWithNonPartialLayoutPath");
        }

        public IActionResult ViewWithPartial_SpecifiedWithPartialName()
        {
            ViewBag.Partial = "NonSharedPartial";
            return View("ViewWithPartials");
        }

        public IActionResult ViewWithPartial_SpecifiedWithAbsoluteName()
        {
            ViewBag.Partial = "~/Views/ViewNameSpecification_Home/NonSharedPartial";
            return View("ViewWithPartials");
        }

        public IActionResult ViewWithPartial_SpecifiedWithAbsoluteNameAndExtension()
        {
            ViewBag.Partial = "~/Views/ViewNameSpecification_Home/NonSharedPartial.cshtml";
            return View("ViewWithPartials");
        }
    }
}