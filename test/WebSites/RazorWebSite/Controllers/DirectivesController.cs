// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace RazorWebSite
{
    public class DirectivesController : Controller
    {
        public ViewResult ViewInheritsInjectAndUsingsFromGlobalImports()
        {
            return View(new Person { Name = "Person1" });
        }

        public ViewResult ViewInheritsBasePageFromGlobalImports()
        {
            return View("/views/directives/scoped/ViewInheritsBasePageFromGlobalImports.cshtml",
                        new Person { Name = "Person2" });
        }
    }
}