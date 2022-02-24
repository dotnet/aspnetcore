// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Components;

public class PassThroughViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(long value)
    {
        return View(value);
    }
}
