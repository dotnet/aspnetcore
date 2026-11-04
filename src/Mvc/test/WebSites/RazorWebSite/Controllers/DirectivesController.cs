// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RazorWebSite;

public class DirectivesController : Controller
{
    public IActionResult ViewInheritsInjectAndUsingsFromViewImports()
    {
        return View(new Person { Name = "Person1" });
    }

    public IActionResult ViewInheritsBasePageFromViewImports()
    {
        return View("/Views/Directives/Scoped/ViewInheritsBasePageFromViewImports.cshtml",
                    new Person { Name = "Person2" });
    }

    public IActionResult ViewReplacesTModelTokenFromInheritedBasePages()
    {
        var model = new Person
        {
            Name = "Bob",
            Address = new Address
            {
                ZipCode = "98052"
            }
        };

        return View("/Views/InheritingInherits/Index.cshtml", model);
    }
}
