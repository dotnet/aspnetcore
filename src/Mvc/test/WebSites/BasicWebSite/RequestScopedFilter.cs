// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BasicWebSite;

public class RequestScopedFilter : IActionFilter
{
    private readonly RequestIdService _requestIdService;

    public RequestScopedFilter(RequestIdService requestIdService)
    {
        _requestIdService = requestIdService;
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        throw new NotImplementedException();
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        context.Result = new ObjectResult(_requestIdService.RequestId);
    }
}
