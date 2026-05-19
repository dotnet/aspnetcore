// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Filters;

namespace BasicWebSite;

public class ServiceActionFilter : IActionFilter
{
    private readonly ILogger<ServiceActionFilter> _logger;

    public ServiceActionFilter(ILogger<ServiceActionFilter> logger)
    {
        _logger = logger;
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        _logger.LogInformation($"Executing {nameof(ServiceActionFilter)}.");
        context.HttpContext.Response.Headers["X-ServiceActionFilter"] = "True";
    }
}
