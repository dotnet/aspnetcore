// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Filters;

namespace FiltersWebSite
{
    public class ControllerActionFilter : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.ActionDescriptor.DisplayName == "FiltersWebSite.ProductsController.GetPrice")
            {
                context.HttpContext.Response.Headers.Append("filters",
                    "On Controller Action Filter - OnActionExecuted");
            }
            else
            {
                context.Result = Helpers.GetContentResult(context.Result, "Controller Action filter - OnActionExecuted");
            }
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.ActionDescriptor.DisplayName == "FiltersWebSite.ProductsController.GetPrice")
            {
                context.HttpContext.Response.Headers.Append("filters",
                    "On Controller Action Filter - OnActionExecuting");
            }

            if (context.ActionDescriptor.DisplayName == "FiltersWebSite.ActionFilterController.GetHelloWorld")
            {
                (context.ActionArguments["fromGlobalActionFilter"] as List<ContentResult>)
                    .Add(Helpers.GetContentResult(context.Result, "Controller Action filter - OnActionExecuting"));
            }
        }
    }
}