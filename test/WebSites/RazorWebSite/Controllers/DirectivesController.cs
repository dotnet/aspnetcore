// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace RazorWebSite
{
    public class DirectivesController : Controller
    {
        public ViewResult ViewInheritsInjectAndUsingsFromViewImports()
        {
            return View(new Person { Name = "Person1" });
        }

        public ViewResult ViewInheritsBasePageFromViewImports()
        {
            return View("/views/directives/scoped/ViewInheritsBasePageFromViewImports.cshtml",
                        new Person { Name = "Person2" });
        }
    }
}