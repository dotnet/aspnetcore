// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace RazorWebSite.Controllers
{
    public class NestedGlobalImportsController : Controller
    {
        public ViewResult Index()
        {
            var model = new Person
            {
                Name = "Controller-Person"
            };

            return View("~/Views/NestedGlobalImports/Nested/Index.cshtml", model);
        }
    }
}