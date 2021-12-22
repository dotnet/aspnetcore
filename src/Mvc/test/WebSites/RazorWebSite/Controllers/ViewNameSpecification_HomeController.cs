// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RazorWebSite.Controllers;

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

    public IActionResult LayoutSpecifiedWithPartialPathInViewStart_ForViewSpecifiedWithRelativePath()
    {
        return View("Views/ViewNameSpecification_Home/LayoutSpecifiedWithPartialPathInViewStart.cshtml");
    }

    public IActionResult LayoutSpecifiedWithPartialPathInViewStart_ForViewSpecifiedWithAppRelativePath()
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

    public IActionResult LayoutSpecifiedWithPartialPathInPageWithRelativePath()
    {
        return View("Views/ViewNameSpecification_Home/LayoutSpecifiedWithPartialPathInPage.cshtml");
    }

    public IActionResult LayoutSpecifiedWithPartialPathInPageWithAppRelativePath()
    {
        return View("~/Views/ViewNameSpecification_Home/LayoutSpecifiedWithPartialPathInPage.cshtml");
    }

    public IActionResult LayoutSpecifiedWithRelativePath()
    {
        ViewData["Layout"] = "_NonSharedLayout.cshtml";
        return View("PageWithNonPartialLayoutPath");
    }

    public IActionResult LayoutSpecifiedWithAppRelativePath()
    {
        ViewData["Layout"] = "~/Views/ViewNameSpecification_Home/_NonSharedLayout.cshtml";
        return View("PageWithNonPartialLayoutPath");
    }

    public IActionResult ViewWithPartial_SpecifiedWithPartialName()
    {
        ViewBag.Partial = "NonSharedPartial";
        return View("ViewWithPartials");
    }

    public IActionResult ViewWithPartial_SpecifiedWithRelativePath()
    {
        ViewBag.Partial = "NonSharedPartial.cshtml";
        return View("ViewWithPartials");
    }

    public IActionResult ViewWithPartial_SpecifiedWithAppRelativePath()
    {
        ViewBag.Partial = "~/Views/ViewNameSpecification_Home/NonSharedPartial.cshtml";
        return View("ViewWithPartials");
    }
}
