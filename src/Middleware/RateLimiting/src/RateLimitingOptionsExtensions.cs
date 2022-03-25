// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;

namespace Microsoft.AspNetCore.RateLimiting;
public static class RateLimitingOptionsExtensions
{
    /// <summary>
    /// Adds a new <see cref="TokenBucketRateLimiter"/> with the given <see cref="TokenBucketRateLimiterOptions"/> to the <see cref="RateLimitingOptions"/>.
    /// </summary>
    /// <param name="options">The <see cref="RateLimitingOptions"/> to add a limiter to.</param>
    /// <param name="name">The name that will be associated with the limiter.</param>
    /// <param name="tokenBucketRateLimiterOptions">The <see cref="TokenBucketRateLimiterOptions"/> to be used for the limiter.</param>
    /// <returns>This <see cref="RateLimitingOptions"/>.</returns>
    public static RateLimitingOptions AddTokenBucketRateLimiter(this RateLimitingOptions options, string name, TokenBucketRateLimiterOptions tokenBucketRateLimiterOptions)
    {
        return options.AddLimiter(name, new TokenBucketRateLimiter(tokenBucketRateLimiterOptions));
    }

    /// <summary>
    /// Adds a new <see cref="ConcurrencyLimiter"/> with the given <see cref="ConcurrencyLimiterOptions"/> to the <see cref="RateLimitingOptions"/>.
    /// </summary>
    /// <param name="options">The <see cref="RateLimitingOptions"/> to add a limiter to.</param>
    /// <param name="name">The name that will be associated with the limiter.</param>
    /// <param name="concurrencyLimiterOptions">The <see cref="ConcurrencyLimiterOptions"/> to be used for the limiter.</param>
    /// <returns>This <see cref="RateLimitingOptions"/>.</returns>
    public static RateLimitingOptions AddConcurrencyLimiter(this RateLimitingOptions options, string name, ConcurrencyLimiterOptions concurrencyLimiterOptions)
    {
        return options.AddLimiter(name, new ConcurrencyLimiter(concurrencyLimiterOptions));
    }
}
