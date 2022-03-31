// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.RateLimiting;
public partial class RateLimitingMiddleware
{

    private readonly RequestDelegate _next;
    private readonly RequestDelegate _onRejected;
    private readonly ILogger _logger;
    private readonly PartitionedRateLimiter<HttpContext> _limiter;
    private RateLimitLease? _lease;

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
    public async Task Invoke(HttpContext context)
    {
        var acquireLeaseTask = TryAcquireAsync(context);

        // Make sure we only ever call GetResult once on the TryEnterAsync ValueTask b/c it resets.
        bool result;

        if (acquireLeaseTask.IsCompleted)
        {
            result = acquireLeaseTask.Result;
        }
        else
        {
            result = await acquireLeaseTask;
        }

        if (result)
        {
            try
            {
                await _next(context);
            }
            finally
            {
                OnCompletion();
            }
        }
        else
        {
            RateLimiterLog.RequestRejectedLimitsExceeded(_logger);
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await _onRejected(context);
        }
    }

    private ValueTask<bool> TryAcquireAsync(HttpContext context)
    {
        // a return value of 'false' indicates that the request is rejected
        // a return value of 'true' indicates that the request may proceed

        var lease = _limiter.Acquire(context);
        if (lease.IsAcquired)
        {
            _lease = lease;
            return ValueTask.FromResult(true);
        }

        var task = _limiter.WaitAsync(context);
        if (task.IsCompletedSuccessfully)
        {
            lease = task.Result;
            if (lease.IsAcquired)
            {
                _lease = lease;
                return ValueTask.FromResult(true);
            }

            return ValueTask.FromResult(false);
        }

        return Awaited(task);
    }

    private void OnCompletion()
    {
        if (_lease != null)
        {
            _lease.Dispose();
        }
    }

    private async ValueTask<bool> Awaited(ValueTask<RateLimitLease> task)
    {
        var lease = await task;

        if (lease.IsAcquired)
        {
            _lease = lease;
            return true;
        }

        return false;
    }

    private static partial class RateLimiterLog
    {
        [LoggerMessage(1, LogLevel.Debug, "Rate limits exceeded, rejecting this request with a '503 server not available' error", EventName = "RequestRejectedLimitsExceeded")]
        internal static partial void RequestRejectedLimitsExceeded(ILogger logger);
    }
}
