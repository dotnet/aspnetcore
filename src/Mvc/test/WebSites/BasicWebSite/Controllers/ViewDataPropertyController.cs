// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicWebSite.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BasicWebSite.Controllers;

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
