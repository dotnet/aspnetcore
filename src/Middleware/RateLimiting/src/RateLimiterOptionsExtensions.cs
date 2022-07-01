// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;

namespace Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Extension methods for the RateLimiting middleware options.
/// </summary>
public static class RateLimiterOptionsExtensions
{
    /// <summary>
    /// Adds a new <see cref="TokenBucketRateLimiter"/> with the given <see cref="TokenBucketRateLimiterOptions"/> to the <see cref="RateLimiterOptions"/>.
    /// </summary>
    /// <param name="options">The <see cref="RateLimiterOptions"/> to add a limiter to.</param>
    /// <param name="policyName">The name that will be associated with the limiter.</param>
    /// <param name="tokenBucketRateLimiterOptions">The <see cref="TokenBucketRateLimiterOptions"/> to be used for the limiter.</param>
    /// <returns>This <see cref="RateLimiterOptions"/>.</returns>
    public static RateLimiterOptions AddTokenBucketLimiter(this RateLimiterOptions options, string policyName, TokenBucketRateLimiterOptions tokenBucketRateLimiterOptions)
    {
        var key = new PolicyNameKey() { PolicyName = policyName };
        return options.AddPolicy(policyName, context =>
        {
            return RateLimitPartition.CreateTokenBucketLimiter(key,
                _ => tokenBucketRateLimiterOptions);
        });
    }

    /// <summary>
    /// Adds a new <see cref="FixedWindowRateLimiter"/> with the given <see cref="FixedWindowRateLimiterOptions"/> to the <see cref="RateLimiterOptions"/>.
    /// </summary>
    /// <param name="options">The <see cref="RateLimiterOptions"/> to add a limiter to.</param>
    /// <param name="policyName">The name that will be associated with the limiter.</param>
    /// <param name="fixedWindowRateLimiterOptions">The <see cref="FixedWindowRateLimiterOptions"/> to be used for the limiter.</param>
    /// <returns>This <see cref="RateLimiterOptions"/>.</returns>
    public static RateLimiterOptions AddFixedWindowLimiter(this RateLimiterOptions options, string policyName, FixedWindowRateLimiterOptions fixedWindowRateLimiterOptions)
    {
        var key = new PolicyNameKey() { PolicyName = policyName };
        return options.AddPolicy(policyName, context =>
        {
            return RateLimitPartition.CreateFixedWindowLimiter(key,
                _ => fixedWindowRateLimiterOptions);
        });
    }

    /// <summary>
    /// Adds a new <see cref="SlidingWindowRateLimiter"/> with the given <see cref="SlidingWindowRateLimiterOptions"/> to the <see cref="RateLimiterOptions"/>.
    /// </summary>
    /// <param name="options">The <see cref="RateLimiterOptions"/> to add a limiter to.</param>
    /// <param name="policyName">The name that will be associated with the limiter.</param>
    /// <param name="slidingWindowRateLimiterOptions">The <see cref="SlidingWindowRateLimiterOptions"/> to be used for the limiter.</param>
    /// <returns>This <see cref="RateLimiterOptions"/>.</returns>
    public static RateLimiterOptions AddSlidingWindowLimiter(this RateLimiterOptions options, string policyName, SlidingWindowRateLimiterOptions slidingWindowRateLimiterOptions)
    {
        var key = new PolicyNameKey() { PolicyName = policyName };
        return options.AddPolicy(policyName, context =>
        {
            return RateLimitPartition.CreateSlidingWindowLimiter(key,
                _ => slidingWindowRateLimiterOptions);
        });
    }

    /// <summary>
    /// Adds a new <see cref="ConcurrencyLimiter"/> with the given <see cref="ConcurrencyLimiterOptions"/> to the <see cref="RateLimiterOptions"/>.
    /// </summary>
    /// <param name="options">The <see cref="RateLimiterOptions"/> to add a limiter to.</param>
    /// <param name="policyName">The name that will be associated with the limiter.</param>
    /// <param name="concurrencyLimiterOptions">The <see cref="ConcurrencyLimiterOptions"/> to be used for the limiter.</param>
    /// <returns>This <see cref="RateLimiterOptions"/>.</returns>
    public static RateLimiterOptions AddConcurrencyLimiter(this RateLimiterOptions options, string policyName, ConcurrencyLimiterOptions concurrencyLimiterOptions)
    {
        var key = new PolicyNameKey() { PolicyName = policyName };
        return options.AddPolicy(policyName, context =>
        {
            return RateLimitPartition.CreateConcurrencyLimiter(key,
                _ => concurrencyLimiterOptions);
        });
    }

    /// <summary>
    /// Adds a new no-op <see cref="RateLimiter"/> to the <see cref="RateLimiterOptions"/>.
    /// </summary>
    /// <param name="options">The <see cref="RateLimiterOptions"/> to add a limiter to.</param>
    /// <param name="policyName">The name that will be associated with the limiter.</param>
    /// <returns>This <see cref="RateLimiterOptions"/>.</returns>
    public static RateLimiterOptions AddNoLimiter(this RateLimiterOptions options, string policyName)
    {
        var key = new PolicyNameKey() { PolicyName = policyName };
        return options.AddPolicy(policyName, context =>
        {
            return RateLimitPartition.CreateNoLimiter(key);
        });
    }
}
