// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace RazorWebSite
{
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
    }
}