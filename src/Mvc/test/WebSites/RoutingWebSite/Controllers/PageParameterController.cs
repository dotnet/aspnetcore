// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite.Controllers;

public class PageParameterController : Controller
{
    // We've had issues with using 'page' as a parameter in tandem with conventional
    // routing + razor pages.
    public ActionResult PageParameter(string page)
    {
        return Content($"page={page}");
    }

    [HttpGet("/PageParameter/LinkToPageParameter")]
    public ActionResult LinkToPageParameter()
    {
        return Content(Url.Action(nameof(PageParameter), new { page = "17", }));
    }
}
