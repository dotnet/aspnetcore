// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

namespace MvcSample.Web.Filters
{
    public class DelayAttribute : ActionFilterAttribute
    {
        public DelayAttribute(int milliseconds)
        {
            Delay = TimeSpan.FromMilliseconds(milliseconds);
        }

        public TimeSpan Delay { get; private set; }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.HttpContext.Request.Method == "GET")
            {
                // slow down incoming GET requests
                await Task.Delay(Delay);
            }

            var executedContext = await next();

            if (executedContext.Result is ViewResult)
            {
                // slow down outgoing view results
                await Task.Delay(Delay);
            }
        }
    }
}