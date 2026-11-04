// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RazorWebSite.Controllers;

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
