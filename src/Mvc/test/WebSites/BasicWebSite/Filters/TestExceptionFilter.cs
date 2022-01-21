// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BasicWebSite.Filters;

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
