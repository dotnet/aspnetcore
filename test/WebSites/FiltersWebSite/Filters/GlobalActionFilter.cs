// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Filters;

namespace FiltersWebSite
{
    public class GlobalActionFilter : IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.ActionDescriptor.DisplayName == "FiltersWebSite.ActionFilterController.GetHelloWorld")
            {
                context.Result = Helpers.GetContentResult(context.Result, "GlobalActionFilter.OnActionExecuted");
            }

            if (context.ActionDescriptor.DisplayName == "FiltersWebSite.ProductsController.GetPrice")
            {
                context.HttpContext.Response.Headers.Append("filters",
                    "Global Action Filter - OnActionExecuted");
            }
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.ActionDescriptor.DisplayName == "FiltersWebSite.ActionFilterController.GetHelloWorld")
            {
                (context.ActionArguments["fromGlobalActionFilter"] as List<ContentResult>)
                    .Add(Helpers.GetContentResult(null, "GlobalActionFilter.OnActionExecuting"));
            }

            if (context.ActionDescriptor.DisplayName == "FiltersWebSite.ProductsController.GetPrice")
            {
                context.HttpContext.Response.Headers.Append("filters",
                    "Global Action Filter - OnActionExecuting");
            }
        }
    }
}