// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RequestThrottling;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Aspnetcore.RequestThrottling
{
    /// <summary>
    /// Limits the flow of <see cref="HttpContext"/>s through your application,
    /// hopefully decreasing congestion in the form of threadpool starvation.
    /// </summary>
    public class RequestThrottlingMiddleware
    {
        private SemaphoreWrapper _semaphore;

        private readonly RequestThrottlingOptions _options;
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new <see cref="RequestThrottlingMiddleware"/>.
        /// </summary>
        /// <param name="next">The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> used for logging.</param>
        /// <param name="options">The <see cref="RequestThrottlingOptions"/> containing the initialization parameters.</param>
        public RequestThrottlingMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<RequestThrottlingOptions> options)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<RequestThrottlingMiddleware>();
            _options = options.Value;
            _semaphore = new SemaphoreWrapper(_options.MaxConcurrentRequests); 
        }

        /// <summary>
        /// Invokes the logic of the middleware.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the request leaves.</returns>
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _semaphore.EnterQueue();
                _logger.LogDebug("EnteredQueue");

                await _next(context);
            }
            finally
            {
                _semaphore.LeaveQueue();
                _logger.LogDebug($"request finished, semaphore count at: {_semaphore.Count}");
            }
        }

        /// <summary>
        /// The number of live requests that are downstream from this middleware.
        /// Cannot exceeed `_options.MaxConcurrentRequests`
        /// </summary>
        public int ConcurrentRequests
        {
            get => (_options.MaxConcurrentRequests - _semaphore.Count);
        }
    }
}
