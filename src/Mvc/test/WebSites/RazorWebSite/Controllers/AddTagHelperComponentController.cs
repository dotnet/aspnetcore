// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;

namespace RazorWebSite.Controllers
{
    public class AddTagHelperComponentController : Controller
    {
        private readonly ITagHelperComponentManager _tagHelperComponentManager;

        public AddTagHelperComponentController(ITagHelperComponentManager tagHelperComponentManager)
        {
            _tagHelperComponentManager = tagHelperComponentManager;
        }

        public IActionResult AddComponent()
        {
            _tagHelperComponentManager.Components.Add(new TestBodyTagHelperComponent(0, "Processed TagHelperComponent added from controller."));
            ViewData["TestData"] = "Value";
            return View("AddComponent");
        }
    }
}
