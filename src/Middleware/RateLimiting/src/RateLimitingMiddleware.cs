// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Limits the rate of requests allowed in the application, based on limits set by a user-provided <see cref="PartitionedRateLimiter{TResource}"/>.
/// </summary>
internal sealed partial class RateLimitingMiddleware
{

    private readonly RequestDelegate _next;
    private readonly RequestDelegate _onRejected;
    private readonly ILogger _logger;
    private readonly PartitionedRateLimiter<HttpContext> _limiter;

    /// <summary>
    /// Creates a new <see cref="RateLimitingMiddleware"/>.
    /// </summary>
    /// <param name="next">The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> used for logging.</param>
    /// <param name="options">The options for the middleware.</param>
    public RateLimitingMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<RateLimitingOptions> options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));

        if (options.Value.Limiter == null)
        {
            throw new ArgumentException("The value of 'options.Limiter' must not be null.", nameof(options));
        }

        if (options.Value.OnRejected == null)
        {
            throw new ArgumentException("The value of 'options.OnRejected' must not be null.", nameof(options));
        }

        if (loggerFactory == null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        _logger = loggerFactory.CreateLogger<RateLimitingMiddleware>();
        _limiter = options.Value.Limiter;
        _onRejected = options.Value.OnRejected;
    }

    // TODO - EventSource?
    /// <summary>
    /// Invokes the logic of the middleware.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>A <see cref="Task"/> that completes when the request leaves.</returns>
    public async Task Invoke(HttpContext context)
    {
        var acquireLeaseTask = TryAcquireAsync(context);

        // Make sure we only ever call GetResult once on the TryEnterAsync ValueTask b/c it resets.
        bool result;
        RateLimitLease lease;

        if (acquireLeaseTask.IsCompleted)
        {
            lease = acquireLeaseTask.Result;
        }
        else
        {
            lease = await acquireLeaseTask;
        }
        result = lease.IsAcquired;

        if (result)
        {
            try
            {
                await _next(context);
            }
            finally
            {
                OnCompletion(lease);
            }
        }
        else
        {
            RateLimiterLog.RequestRejectedLimitsExceeded(_logger);
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await _onRejected(context);
        }
    }

    private ValueTask<RateLimitLease> TryAcquireAsync(HttpContext context)
    {
        var lease = _limiter.Acquire(context);
        if (lease.IsAcquired)
        {
            return ValueTask.FromResult(lease);
        }

        var task = _limiter.WaitAsync(context);
        if (task.IsCompletedSuccessfully)
        {
            return task;
        }

        return Awaited(task);
    }

    private static void OnCompletion(RateLimitLease lease)
    {
        if (lease != null)
        {
            lease.Dispose();
        }
    }

    private static async ValueTask<RateLimitLease> Awaited(ValueTask<RateLimitLease> task)
    {
        return await task;
    }

    private static partial class RateLimiterLog
    {
        [LoggerMessage(1, LogLevel.Debug, "Rate limits exceeded, rejecting this request with a '503 server not available' error", EventName = "RequestRejectedLimitsExceeded")]
        internal static partial void RequestRejectedLimitsExceeded(ILogger logger);
    }
}
