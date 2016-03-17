// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FiltersWebSite
{
    public class GlobalResultFilter : IResultFilter
    {
        public void OnResultExecuted(ResultExecutedContext context)
        {
            var controllerActionDescriptor = (ControllerActionDescriptor)context.ActionDescriptor;
            if (controllerActionDescriptor.MethodInfo ==
                typeof(ProductsController).GetMethod(nameof(ProductsController.GetPrice)))
            {
                context.HttpContext.Response.Headers.Append("filters",
                    "Global Result Filter - OnResultExecuted");
            }
        }

        public void OnResultExecuting(ResultExecutingContext context)
        {
            var controllerActionDescriptor = (ControllerActionDescriptor)context.ActionDescriptor;
            if (controllerActionDescriptor.MethodInfo ==
                typeof(ResultFilterController).GetMethod(nameof(ResultFilterController.GetHelloWorld)))
            {
                context.Result = Helpers.GetContentResult(context.Result, "GlobalResultFilter.OnResultExecuting");
            }

            if (controllerActionDescriptor.MethodInfo ==
                typeof(ProductsController).GetMethod(nameof(ProductsController.GetPrice)))
            {
                context.HttpContext.Response.Headers.Append("filters",
                    "Global Result Filter - OnResultExecuted");
            }
        }
    }
}