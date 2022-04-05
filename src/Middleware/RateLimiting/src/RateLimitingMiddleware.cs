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
    /// <param name="logger">The <see cref="ILogger"/> used for logging.</param>
    /// <param name="options">The options for the middleware.</param>
    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, IOptions<RateLimitingOptions> options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));

        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        _logger = logger;
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
        using var lease = await TryAcquireAsync(context);
        try
        {
            if (lease.IsAcquired)
            {
                await _next(context);
            }
            else
            {
                RateLimiterLog.RequestRejectedLimitsExceeded(_logger);
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await _onRejected(context);
            }
        }
        finally
        {
            OnCompletion(lease);
        }
    }

    private ValueTask<RateLimitLease> TryAcquireAsync(HttpContext context)
    {
        var lease = _limiter.Acquire(context);
        if (lease.IsAcquired)
        {
            return ValueTask.FromResult(lease);
        }

        return _limiter.WaitAsync(context, cancellationToken: context.RequestAborted);
    }

    private static void OnCompletion(RateLimitLease lease) => lease?.Dispose();

    private static partial class RateLimiterLog
    {
        [LoggerMessage(1, LogLevel.Debug, "Rate limits exceeded, rejecting this request with a '503 server not available' error", EventName = "RequestRejectedLimitsExceeded")]
        internal static partial void RequestRejectedLimitsExceeded(ILogger logger);
    }
}
