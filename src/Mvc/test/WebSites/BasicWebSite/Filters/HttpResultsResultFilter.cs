// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BasicWebSite;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class HttpResultsFilter : Attribute, IResultFilter
{
    public void OnResultExecuted(ResultExecutedContext context)
    {
    }

    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context is { Result: HttpActionResult { Result: IResult result } })
        {
            context.Result = new StatusCodeResult(StatusCodes.Status200OK);
            context.HttpContext.Response.Headers["X-HttpResultType"] = result.GetType().Name;
        }
    }
}
