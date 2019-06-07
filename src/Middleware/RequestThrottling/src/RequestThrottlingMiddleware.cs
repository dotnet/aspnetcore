// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RequestThrottling.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.RequestThrottling
{
    /// <summary>
    /// Limits the number of concurrent requests allowed in the application.
    /// </summary>
    public class RequestThrottlingMiddleware
    {
        private readonly RequestQueue _requestQueue;
        private readonly RequestDelegate _next;
        private readonly RequestThrottlingOptions _requestThrottlingOptions;
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new <see cref="RequestThrottlingMiddleware"/>.
        /// </summary>
        /// <param name="next">The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> used for logging.</param>
        /// <param name="options">The <see cref="RequestThrottlingOptions"/> containing the initialization parameters.</param>
        public RequestThrottlingMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<RequestThrottlingOptions> options)
        {
            _requestThrottlingOptions = options.Value;

            if (_requestThrottlingOptions.MaxConcurrentRequests == null)
            {
                throw new ArgumentException("The value of 'options.MaxConcurrentRequests' must be specified.", nameof(options));
            }
            if (_requestThrottlingOptions.MaxConcurrentRequests <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options), "The value of `options.MaxConcurrentRequests` must be a positive integer.");
            }
            if (_requestThrottlingOptions.RequestQueueLimit < 0)
            {
                throw new ArgumentException("The value of 'options.RequestQueueLimit' must be a positive integer.", nameof(options));
            }

            if (_requestThrottlingOptions.OnRejected == null)
            {
                throw new ArgumentException("The value of 'options.OnRejected' must not be null.", nameof(options));
            }

            _next = next;
            _logger = loggerFactory.CreateLogger<RequestThrottlingMiddleware>();

            if (_requestThrottlingOptions.ServerAlwaysBlocks)
            {
                _requestQueue = new TailDrop(0, _requestThrottlingOptions.RequestQueueLimit);
            }
            else
            {
                _requestQueue = new TailDrop(_requestThrottlingOptions.MaxConcurrentRequests.Value, _requestThrottlingOptions.RequestQueueLimit);
            }
        }

        /// <summary>
        /// Invokes the logic of the middleware.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the request leaves.</returns>
        public async Task Invoke(HttpContext context)
        {
            var waitInQueueTask = _requestQueue.TryEnterQueueAsync();

            if (waitInQueueTask.IsCompletedSuccessfully && waitInQueueTask.Result)
            {
                RequestThrottlingLog.RequestRunImmediately(_logger, ActiveRequestCount);
            }
            else
            {
                RequestThrottlingLog.RequestEnqueued(_logger, ActiveRequestCount);
                await waitInQueueTask;
                RequestThrottlingLog.RequestDequeued(_logger, ActiveRequestCount);
            }

            if (!waitInQueueTask.Result)
            {
                RequestThrottlingLog.RequestRejectedQueueFull(_logger);
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await _requestThrottlingOptions.OnRejected(context);
                return;
            }

            try
            {
                await _next(context);
            }
            finally
            {
                _requestQueue.Release();
            }
        }

        /// <summary>
        /// The number of requests currently on the server.
        /// Cannot exceeed the sum of <see cref="RequestThrottlingOptions.RequestQueueLimit"> and </see>/><see cref="RequestThrottlingOptions.MaxConcurrentRequests"/>.
        /// </summary>
        public int ActiveRequestCount
        {
            get => _requestQueue.TotalRequests;
        }

        // TODO :: update log wording to reflect the changes

        private static class RequestThrottlingLog
        {
            private static readonly Action<ILogger, int, Exception> _requestEnqueued =
                LoggerMessage.Define<int>(LogLevel.Debug, new EventId(1, "RequestEnqueued"), "MaxConcurrentRequests limit reached, request has been queued. Current active requests: {ActiveRequests}.");

            private static readonly Action<ILogger, int, Exception> _requestDequeued =
                LoggerMessage.Define<int>(LogLevel.Debug, new EventId(2, "RequestDequeued"), "Request dequeued. Current active requests: {ActiveRequests}.");

            private static readonly Action<ILogger, int, Exception> _requestRunImmediately =
                LoggerMessage.Define<int>(LogLevel.Debug, new EventId(3, "RequestRunImmediately"), "Below MaxConcurrentRequests limit, running request immediately. Current active requests: {ActiveRequests}");

            private static readonly Action<ILogger, Exception> _requestRejectedQueueFull =
                LoggerMessage.Define(LogLevel.Debug, new EventId(4, "RequestRejectedQueueFull"), "Currently at the 'RequestQueueLimit', rejecting this request with a '503 server not availible' error");

            internal static void RequestEnqueued(ILogger logger, int activeRequests)
            {
                _requestEnqueued(logger, activeRequests, null);
            }

            internal static void RequestDequeued(ILogger logger, int activeRequests)
            {
                _requestDequeued(logger, activeRequests, null);
            }

            internal static void RequestRunImmediately(ILogger logger, int activeRequests)
            {
                _requestRunImmediately(logger, activeRequests, null);
            }

            internal static void RequestRejectedQueueFull(ILogger logger)
            {
                _requestRejectedQueueFull(logger, null);
            }
        }
    }
}
