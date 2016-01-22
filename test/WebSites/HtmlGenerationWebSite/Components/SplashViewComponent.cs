// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace MvcSample.Web.Components
{
    public class SplashViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var region = (string)ViewData["Locale"];
            var model = region == "North" ? "NorthWest Store":
                                            "Nationwide Store";

            return View(model: model);
        }
    }
}