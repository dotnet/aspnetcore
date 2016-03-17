// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FiltersWebSite
{
    public class PassThroughResultFilter : ResultFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            var controllerActionDescriptor = (ControllerActionDescriptor)context.ActionDescriptor;
            if (controllerActionDescriptor.MethodInfo ==
                typeof(ProductsController).GetMethod(nameof(ProductsController.GetPrice)))
            {
                context.HttpContext.Response.Headers.Append("filters",
                    "On Action Result Filter - OnResultExecuting");
            }
        }

        public override void OnResultExecuted(ResultExecutedContext context)
        {
            var controllerActionDescriptor = (ControllerActionDescriptor)context.ActionDescriptor;
            if (controllerActionDescriptor.MethodInfo ==
                typeof(ProductsController).GetMethod(nameof(ProductsController.GetPrice)))
            {
                context.HttpContext.Response.Headers.Append("filters",
                    "On Action Result Filter - OnResultExecuted");
            }
        }
    }
}