// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite.Areas.Admin
{
    [Area("Admin")]
    public class FallbackController : Controller
    {
        public ActionResult Index()
        {
            return Content("Hello from fallback controller: " + Url.Action());
        }

        [HttpPost]
        public ActionResult Index(int x = 0)
        {
            return Content("Hello from fallback controller POST: " + Url.Action());
        }
    }
}
