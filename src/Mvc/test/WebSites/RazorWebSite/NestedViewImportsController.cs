// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace RazorWebSite.Controllers
{
    public class NestedViewImportsController : Controller
    {
        public ViewResult Index()
        {
            var model = new Person
            {
                Name = "Controller-Person"
            };

            return View("~/Views/NestedViewImports/Nested/Index.cshtml", model);
        }
    }
}