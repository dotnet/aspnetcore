// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.RequestThrottling
{
    /// <summary>
    /// Limits the number of concurrent requests allowed in the application.
    /// </summary>
    public class RequestThrottlingMiddleware
    {
        private readonly IQueuePolicy _queuePolicy;
        private readonly RequestDelegate _next;
        private readonly RequestDelegate _onRejected;
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new <see cref="RequestThrottlingMiddleware"/>.
        /// </summary>
        /// <param name="next">The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> used for logging.</param>
        /// <param name="queue">The queueing strategy to use for the server.</param>
        /// <param name="options">The options for the middleware, currently containing the 'OnRejected' callback.</param>
        public RequestThrottlingMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IQueuePolicy queue, IOptions<RequestThrottlingOptions> options)
        {
            if (options.Value.OnRejected == null)
            {
                throw new ArgumentException("The value of 'options.OnRejected' must not be null.", nameof(options));
            }

            _next = next;
            _logger = loggerFactory.CreateLogger<RequestThrottlingMiddleware>();
            _onRejected = options.Value.OnRejected;
            _queuePolicy = queue;
        }

        /// <summary>
        /// Invokes the logic of the middleware.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the request leaves.</returns>
        public async Task Invoke(HttpContext context)
        {
            var waitInQueueTask = _queuePolicy.TryEnterAsync();

            if (waitInQueueTask.IsCompleted)
            {
                RequestThrottlingEventSource.Log.QueueSkipped();
            }
            else
            {
                using (RequestThrottlingEventSource.Log.QueueTimer())
                {
                    await waitInQueueTask;
                }
            }

            if (waitInQueueTask.Result)
            {
                try
                {
                    await _next(context);
                }
                finally
                {
                    _queuePolicy.OnExit();
                }
            }
            else
            {
                RequestThrottlingEventSource.Log.RequestRejected();
                RequestThrottlingLog.RequestRejectedQueueFull(_logger);
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await _onRejected(context);
            }
        }

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
