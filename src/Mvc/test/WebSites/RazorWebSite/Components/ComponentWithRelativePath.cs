// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RazorWebSite.Components;

public class ComponentWithRelativePath : ViewComponent
{
    public IViewComponentResult Invoke(Person person)
    {
        return View("../Shared/Components/ComponentWithRelativePath.cshtml", person);
    }
}
