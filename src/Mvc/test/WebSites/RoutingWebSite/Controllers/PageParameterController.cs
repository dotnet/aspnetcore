// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite.Controllers
{
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
}
