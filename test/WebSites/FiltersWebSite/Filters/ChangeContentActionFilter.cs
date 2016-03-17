// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FiltersWebSite
{
    public class ChangeContentActionFilter : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            context.Result = Helpers.GetContentResult(context.Result, "Action Filter - OnActionExecuted");
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var controllerActionDescriptor = (ControllerActionDescriptor)context.ActionDescriptor;
            if (controllerActionDescriptor.MethodInfo ==
                typeof(ActionFilterController).GetMethod(nameof(ActionFilterController.GetHelloWorld)))
            {
                (context.ActionArguments["fromGlobalActionFilter"] as List<ContentResult>).
                    Add(Helpers.GetContentResult(context.Result, "Action Filter - OnActionExecuting"));
            }
        }
    }
}