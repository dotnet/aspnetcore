// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicWebSite.Components;
using BasicWebSite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace BasicWebSite.Controllers
{
    public class ViewDataPropertyController : Controller
    {
        [ViewData]
        public string Title => "View Data Property Sample";

        [ViewData]
        public string Message { get; private set; }

        [ViewData]
        public string FilterMessage { get; set; }

        public IActionResult ViewDataPropertyToView()
        {
            Message = "Message set in action";
            return View();
        }

        public IActionResult ViewDataPropertyToViewComponent()
        {
            Message = "Message set in action";
            return ViewComponent(typeof(ViewDataViewComponent));
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            FilterMessage = "Value set in OnActionExecuting";
        }
    }
}
