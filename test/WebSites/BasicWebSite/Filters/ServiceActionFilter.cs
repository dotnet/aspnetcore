// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace BasicWebSite
{
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
}
