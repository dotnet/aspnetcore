// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
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
        private readonly SemaphoreSlim _queueSemaphore;
        private readonly SemaphoreSlim _serverSemaphore;
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly RequestThrottlingOptions _options;

        /// <summary>
        /// Creates a new <see cref="RequestThrottlingMiddleware"/>.
        /// </summary>
        /// <param name="next">The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> used for logging.</param>
        /// <param name="options">The <see cref="RequestThrottlingOptions"/> containing the initialization parameters.</param>
        public RequestThrottlingMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<RequestThrottlingOptions> options)
        {
            if (options.Value.MaxConcurrentRequests == null)
            {
                throw new ArgumentException("The value of 'options.MaxConcurrentRequests' must be specified.", nameof(options));
            }
            if (options.Value.RequestQueueLimit < 0)
            {
                throw new ArgumentException("The value of 'options.RequestQueueLimit' must be a positive integer.", nameof(options));
            }

            _next = next;
            _logger = loggerFactory.CreateLogger<RequestThrottlingMiddleware>();
            _options = options.Value;

            _queueSemaphore = new SemaphoreSlim(_options.RequestQueueLimit);
            _serverSemaphore = new SemaphoreSlim(_options.MaxConcurrentRequests.Value);
        }

        /// <summary>
        /// Invokes the logic of the middleware.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the request leaves.</returns>
        public async Task Invoke(HttpContext context)
        {
            var queueEntryTask = _queueSemaphore.WaitAsync();
            if (!queueEntryTask.IsCompletedSuccessfully)
            {
                RequestThrottlingLog.RequestRejectedQueueFull(_logger);
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                return;
            }

            var serverEntryTask = _serverSemaphore.WaitAsync();
            if (!serverEntryTask.IsCompletedSuccessfully)
            {
                RequestThrottlingLog.RequestEnqueued(_logger, WaitingRequests);
                await serverEntryTask;
                RequestThrottlingLog.RequestDequeued(_logger, WaitingRequests);
            }
            else
            {
                RequestThrottlingLog.RequestRunImmediately(_logger);
            }

            _queueSemaphore.Release();

            try
            {
                await _next(context);
            }
            finally
            {
                _serverSemaphore.Release();
            }
        }

        /// <summary>
        /// The number of live requests that are downstream from this middleware.
        /// Cannot exceeed <see cref="RequestThrottlingOptions.MaxConcurrentRequests"/>.
        /// </summary>
        internal int ConcurrentRequests
        {
            get => _options.MaxConcurrentRequests.Value - _serverSemaphore.CurrentCount;
        }

        /// <summary>
        /// Number of requests currently enqueued and waiting to be processed.
        /// </summary>
        internal int WaitingRequests
        {
            get => _options.RequestQueueLimit - _queueSemaphore.CurrentCount;
        }

        private static class RequestThrottlingLog
        {
            private static readonly Action<ILogger, int, Exception> _requestEnqueued =
                LoggerMessage.Define<int>(LogLevel.Debug, new EventId(1, "RequestEnqueued"), "Concurrent request limit reached, queuing request. Current queue length: {QueuedRequests}.");

            private static readonly Action<ILogger, int, Exception> _requestDequeued =
                LoggerMessage.Define<int>(LogLevel.Debug, new EventId(2, "RequestDequeued"), "Request dequeued. Current queue length: {QueuedRequests}.");

            private static readonly Action<ILogger, Exception> _requestRunImmediately =
                LoggerMessage.Define(LogLevel.Debug, new EventId(3, "RequestRunImmediately"), "Concurrent request limit has not been reached, running request immediately.");

            private static readonly Action<ILogger, Exception> _requestRejectedQueueFull =
                LoggerMessage.Define(LogLevel.Debug, new EventId(4, "RequestRejectedQueueFull"), "Currently at the 'RequestQueueLimit', rejecting this request with a '503 server not availible' error");

            internal static void RequestEnqueued(ILogger logger, int queuedRequests)
            {
                _requestEnqueued(logger, queuedRequests, null);
            }

            internal static void RequestDequeued(ILogger logger, int queuedRequests)
            {
                _requestDequeued(logger, queuedRequests, null);
            }

            internal static void RequestRunImmediately(ILogger logger)
            {
                _requestRunImmediately(logger, null);
            }

            internal static void RequestRejectedQueueFull(ILogger logger)
            {
                _requestRejectedQueueFull(logger, null);
            }
        }
    }
}
