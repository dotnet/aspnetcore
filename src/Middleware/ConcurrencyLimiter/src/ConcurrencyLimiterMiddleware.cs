// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ConcurrencyLimiter
{
    /// <summary>
    /// Limits the number of concurrent requests allowed in the application.
    /// </summary>
    public class ConcurrencyLimiterMiddleware
    {
        private readonly IQueuePolicy _queuePolicy;
        private readonly RequestDelegate _next;
        private readonly RequestDelegate _onRejected;
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new <see cref="ConcurrencyLimiterMiddleware"/>.
        /// </summary>
        /// <param name="next">The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> used for logging.</param>
        /// <param name="queue">The queueing strategy to use for the server.</param>
        /// <param name="options">The options for the middleware, currently containing the 'OnRejected' callback.</param>
        public ConcurrencyLimiterMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IQueuePolicy queue, IOptions<ConcurrencyLimiterOptions> options)
        {
            if (options.Value.OnRejected == null)
            {
                throw new ArgumentException("The value of 'options.OnRejected' must not be null.", nameof(options));
            }

            _next = next;
            _logger = loggerFactory.CreateLogger<ConcurrencyLimiterMiddleware>();
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

            // Make sure we only ever call GetResult once on the TryEnterAsync ValueTask b/c it resets.
            bool result;

            if (waitInQueueTask.IsCompleted)
            {
                ConcurrencyLimiterEventSource.Log.QueueSkipped();
                result = waitInQueueTask.Result;
            }
            else
            {
                using (ConcurrencyLimiterEventSource.Log.QueueTimer())
                {
                    result = await waitInQueueTask;
                }
            }

            if (result)
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
                ConcurrencyLimiterEventSource.Log.RequestRejected();
                ConcurrencyLimiterLog.RequestRejectedQueueFull(_logger);
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await _onRejected(context);
            }
        }

        private static class ConcurrencyLimiterLog
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
