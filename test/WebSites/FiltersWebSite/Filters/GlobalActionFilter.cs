// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FiltersWebSite
{
    public class GlobalActionFilter : IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
            var controllerActionDescriptor = (ControllerActionDescriptor)context.ActionDescriptor;
            if (controllerActionDescriptor.MethodInfo ==
                typeof(ActionFilterController).GetMethod(nameof(ActionFilterController.GetHelloWorld)))
            {
                context.Result = Helpers.GetContentResult(context.Result, "GlobalActionFilter.OnActionExecuted");
            }

            if (controllerActionDescriptor.MethodInfo ==
                typeof(ProductsController).GetMethod(nameof(ProductsController.GetPrice)))
            {
                context.HttpContext.Response.Headers.Append("filters",
                    "Global Action Filter - OnActionExecuted");
            }
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var controllerActionDescriptor = (ControllerActionDescriptor)context.ActionDescriptor;
            if (controllerActionDescriptor.MethodInfo ==
                typeof(ActionFilterController).GetMethod(nameof(ActionFilterController.GetHelloWorld)))
            {
                (context.ActionArguments["fromGlobalActionFilter"] as List<ContentResult>)
                    .Add(Helpers.GetContentResult(null, "GlobalActionFilter.OnActionExecuting"));
            }

            if (controllerActionDescriptor.MethodInfo ==
                typeof(ProductsController).GetMethod(nameof(ProductsController.GetPrice)))
            {
                context.HttpContext.Response.Headers.Append("filters",
                    "Global Action Filter - OnActionExecuting");
            }
        }
    }
}