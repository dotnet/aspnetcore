// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace RateLimitingSample;

public class SampleRateLimiterPolicy : IRateLimiterPolicy<string>
{
    private Func<OnRejectedContext, CancellationToken, ValueTask>? _onRejected;

    public SampleRateLimiterPolicy(ILogger<SampleRateLimiterPolicy> logger)
    {
        _onRejected = (context, token) =>
        {
            context.HttpContext.Response.StatusCode = 429;
            logger.LogInformation($"Request rejected by {nameof(SampleRateLimiterPolicy)}");
            return ValueTask.CompletedTask;
        };
    }

    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected { get => _onRejected; }

    // Use a sliding window limiter allowing 1 request every 10 seconds
    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        return RateLimitPartition.GetSlidingWindowLimiter<string>(string.Empty, key => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 1,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 1,
            Window = TimeSpan.FromSeconds(5),
            SegmentsPerWindow = 1
        });
    }
}
