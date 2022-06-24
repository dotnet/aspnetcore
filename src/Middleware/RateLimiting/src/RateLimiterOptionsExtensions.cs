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
        return options.InternalAddPolicy(policyName, context =>
        {
            return RateLimitPartition.CreateTokenBucketLimiter((DefaultKeyType)new DefaultKeyType<PolicyNameKey>(new PolicyNameKey() { PolicyName = policyName }),
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
        return options.InternalAddPolicy(policyName, context =>
        {
            return RateLimitPartition.CreateFixedWindowLimiter((DefaultKeyType)new DefaultKeyType<PolicyNameKey>(new PolicyNameKey() { PolicyName = policyName }),
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
        return options.InternalAddPolicy(policyName, context =>
        {
            return RateLimitPartition.CreateSlidingWindowLimiter((DefaultKeyType)new DefaultKeyType<PolicyNameKey>(new PolicyNameKey() { PolicyName = policyName }),
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
        return options.InternalAddPolicy(policyName, context =>
        {
            return RateLimitPartition.CreateConcurrencyLimiter((DefaultKeyType)new DefaultKeyType<PolicyNameKey>(new PolicyNameKey() { PolicyName = policyName }),
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
        return options.InternalAddPolicy(policyName, context =>
        {
            return RateLimitPartition.CreateNoLimiter((DefaultKeyType)new DefaultKeyType<PolicyNameKey>(new PolicyNameKey() { PolicyName = policyName }));
        });
    }
}
