// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RateLimiting;
public static class RateLimiterOptionsExtensions
{
    /// <summary>
    /// Adds a new <see cref="TokenBucketRateLimiter"/> with the given <see cref="TokenBucketRateLimiterOptions"/> to the <see cref="RateLimiterOptions"/>.
    /// </summary>
    /// <param name="options">The <see cref="RateLimiterOptions"/> to add a limiter to.</param>
    /// <param name="name">The name that will be associated with the limiter.</param>
    /// <param name="tokenBucketRateLimiterOptions">The <see cref="TokenBucketRateLimiterOptions"/> to be used for the limiter.</param>
    /// <returns>This <see cref="RateLimiterOptions"/>.</returns>
    public static RateLimiterOptions AddTokenBucketRateLimiter(this RateLimiterOptions options, string name, TokenBucketRateLimiterOptions tokenBucketRateLimiterOptions)
    {
        return options.AddLimiter<NoKey>(name, context =>
        {
            return RateLimitPartition.CreateTokenBucketLimiter(NoKey.Instance,
                _ => tokenBucketRateLimiterOptions);
        });
    }

    /// <summary>
    /// Adds a new <see cref="FixedWindowRateLimiter"/> with the given <see cref="FixedWindowRateLimiterOptions"/> to the <see cref="RateLimiterOptions"/>.
    /// </summary>
    /// <param name="options">The <see cref="RateLimiterOptions"/> to add a limiter to.</param>
    /// <param name="name">The name that will be associated with the limiter.</param>
    /// <param name="fixedWindowRateLimiterOptions">The <see cref="FixedWindowRateLimiterOptions"/> to be used for the limiter.</param>
    /// <returns>This <see cref="RateLimiterOptions"/>.</returns>
    public static RateLimiterOptions AddFixedWindowRateLimiter(this RateLimiterOptions options, string name, FixedWindowRateLimiterOptions fixedWindowRateLimiterOptions)
    {
        return options.AddLimiter<NoKey>(name, context =>
        {
            return RateLimitPartition.CreateFixedWindowLimiter(NoKey.Instance,
                _ => fixedWindowRateLimiterOptions);
        });
    }

    /// <summary>
    /// Adds a new <see cref="SlidingWindowRateLimiter"/> with the given <see cref="SlidingWindowRateLimiterOptions"/> to the <see cref="RateLimiterOptions"/>.
    /// </summary>
    /// <param name="options">The <see cref="RateLimiterOptions"/> to add a limiter to.</param>
    /// <param name="name">The name that will be associated with the limiter.</param>
    /// <param name="slidingWindowRateLimiterOptions">The <see cref="SlidingWindowRateLimiterOptions"/> to be used for the limiter.</param>
    /// <returns>This <see cref="RateLimiterOptions"/>.</returns>
    public static RateLimiterOptions AddSlidingWindowRateLimiter(this RateLimiterOptions options, string name, SlidingWindowRateLimiterOptions slidingWindowRateLimiterOptions)
    {
        return options.AddLimiter<NoKey>(name, context =>
        {
            return RateLimitPartition.CreateSlidingWindowLimiter(NoKey.Instance,
                _ => slidingWindowRateLimiterOptions);
        });
    }

    /// <summary>
    /// Adds a new <see cref="ConcurrencyLimiter"/> with the given <see cref="ConcurrencyLimiterOptions"/> to the <see cref="RateLimiterOptions"/>.
    /// </summary>
    /// <param name="options">The <see cref="RateLimiterOptions"/> to add a limiter to.</param>
    /// <param name="name">The name that will be associated with the limiter.</param>
    /// <param name="concurrencyLimiterOptions">The <see cref="ConcurrencyLimiterOptions"/> to be used for the limiter.</param>
    /// <returns>This <see cref="RateLimiterOptions"/>.</returns>
    public static RateLimiterOptions AddConcurrencyLimiter(this RateLimiterOptions options, string name, ConcurrencyLimiterOptions concurrencyLimiterOptions)
    {
        return options.AddLimiter<NoKey>(name, context =>
        {
            return RateLimitPartition.CreateConcurrencyLimiter(NoKey.Instance,
                _ => concurrencyLimiterOptions);
        });
    }
}
