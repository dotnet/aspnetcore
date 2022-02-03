// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace MvcSample.Web.Components;

public class ComponentWithLayout : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        ViewData["Title"] = "ViewComponent With Title";
        return View();
    }
}
