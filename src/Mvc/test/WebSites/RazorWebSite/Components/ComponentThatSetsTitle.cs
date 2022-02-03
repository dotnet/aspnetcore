// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace MvcSample.Web.Components;

[ViewComponent(Name = "ComponentThatSetsTitle")]
public class ComponentThatSetsTitle : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View();
    }
}
