// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace MvcSample.Web.Components;

public class SplashViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var region = (string)ViewData["Locale"];
        var model = region == "North" ? "NorthWest Store" :
                                        "Nationwide Store";

        return View(model: model);
    }
}
