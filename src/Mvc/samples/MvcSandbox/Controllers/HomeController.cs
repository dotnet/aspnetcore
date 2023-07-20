// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace MvcSandbox.Controllers;

public class HomeController : Controller
{
    [ModelBinder]
    public string Id { get; set; }

    public IActionResult Index()
    {
        return View();
    }

    [FromForm(MaxModelBindingCollectionSize = 1)][BindProperty] public TypeWithCollection Value { get; set; }

    public IActionResult SendFormData()
    {
        if (Value.Collection.Count > 2)
        {
            return Content("You have failed me for the last time");
        }
        return Content("Yolo");
    }

    public class TypeWithCollection
    {
        public List<string> Collection { get; set; }
    }
}
