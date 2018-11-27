// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BasicWebSite.Filters
{
    public class TestExceptionFilter : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            if (context.HttpContext.Request.Query.TryGetValue("handleException", out var handleException))
            {
                if (handleException.Equals("true"))
                {
                    context.Result = new ContentResult() { Content = "Exception was handled in TestExceptionFilter", StatusCode = 200 };
                    context.ExceptionHandled = true;
                }
            }
        }
    }
}
