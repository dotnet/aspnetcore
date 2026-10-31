// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using HtmlGenerationWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace HtmlGenerationWebSite.Controllers;

public class HtmlGeneration_WeirdExpressionsController : Controller
{
    public IActionResult GetWeirdWithHtmlHelpers()
    {
        return View(new WeirdModel());
    }

    public IActionResult GetWeirdWithTagHelpers()
    {
        return View(new WeirdModel());
    }
}
