// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RequestThrottling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Aspnetcore.RequestThrottling
{
    public class RequestThrottlingMiddleware
    {
        private SemaphoreWrapper _semaphore;

        private readonly RequestThrottlingOptions _options;
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public RequestThrottlingMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<RequestThrottlingOptions> options)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<RequestThrottlingMiddleware>();
            _options = options.Value;
            _semaphore = new SemaphoreWrapper(_options.MaxConcurrentRequests); 
        }

        public async Task Invoke(HttpContext context)
        {
            await _semaphore.EnterQueue();
            _logger.LogDebug("Entered Queue");

            await _next(context);

            _semaphore.LeaveQueue();
            _logger.LogDebug($"request finished, semaphore count at: {_semaphore.Count}");
        }
    }
}
