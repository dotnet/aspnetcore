// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FiltersWebSite
{
    public class ControllerExceptionFilter : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            if (context.Exception.GetType() == typeof(InvalidOperationException))
            {
                context.Result = Helpers.GetContentResult(context.Result, "ControllerExceptionFilter.OnException");
            }
        }
    }
}