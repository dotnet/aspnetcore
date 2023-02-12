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
    /// <param name="configureOptions">A callback to configure the <see cref="TokenBucketRateLimiterOptions"/> to be used for the limiter.</param>
    /// <returns>This <see cref="RateLimiterOptions"/>.</returns>
    public static RateLimiterOptions AddTokenBucketLimiter(this RateLimiterOptions options, string policyName, Action<TokenBucketRateLimiterOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        var key = new PolicyNameKey() { PolicyName = policyName };
        var tokenBucketRateLimiterOptions = new TokenBucketRateLimiterOptions();
        configureOptions.Invoke(tokenBucketRateLimiterOptions);
        // Saves an allocation in GetTokenBucketLimiter, which would have created a new set of options if this was true.
        tokenBucketRateLimiterOptions.AutoReplenishment = false;
        return options.AddPolicy(policyName, context =>
        {
            return RateLimitPartition.GetTokenBucketLimiter(key,
                _ => tokenBucketRateLimiterOptions);
        });
    }

    /// <summary>
    /// Adds a new <see cref="FixedWindowRateLimiter"/> with the given <see cref="FixedWindowRateLimiterOptions"/> to the <see cref="RateLimiterOptions"/>.
    /// </summary>
    /// <param name="options">The <see cref="RateLimiterOptions"/> to add a limiter to.</param>
    /// <param name="policyName">The name that will be associated with the limiter.</param>
    /// <param name="configureOptions">A callback to configure the <see cref="FixedWindowRateLimiterOptions"/> to be used for the limiter.</param>
    /// <returns>This <see cref="RateLimiterOptions"/>.</returns>
    public static RateLimiterOptions AddFixedWindowLimiter(this RateLimiterOptions options, string policyName, Action<FixedWindowRateLimiterOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        var key = new PolicyNameKey() { PolicyName = policyName };
        var fixedWindowRateLimiterOptions = new FixedWindowRateLimiterOptions();
        configureOptions.Invoke(fixedWindowRateLimiterOptions);
        // Saves an allocation in GetFixedWindowLimiter, which would have created a new set of options if this was true.
        fixedWindowRateLimiterOptions.AutoReplenishment = false;
        return options.AddPolicy(policyName, context =>
        {
            return RateLimitPartition.GetFixedWindowLimiter(key,
                _ => fixedWindowRateLimiterOptions);
        });
    }

    /// <summary>
    /// Adds a new <see cref="SlidingWindowRateLimiter"/> with the given <see cref="SlidingWindowRateLimiterOptions"/> to the <see cref="RateLimiterOptions"/>.
    /// </summary>
    /// <param name="options">The <see cref="RateLimiterOptions"/> to add a limiter to.</param>
    /// <param name="policyName">The name that will be associated with the limiter.</param>
    /// <param name="configureOptions">A callback to configure the <see cref="SlidingWindowRateLimiterOptions"/> to be used for the limiter.</param>
    /// <returns>This <see cref="RateLimiterOptions"/>.</returns>
    public static RateLimiterOptions AddSlidingWindowLimiter(this RateLimiterOptions options, string policyName, Action<SlidingWindowRateLimiterOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        var key = new PolicyNameKey() { PolicyName = policyName };
        var slidingWindowRateLimiterOptions = new SlidingWindowRateLimiterOptions();
        configureOptions.Invoke(slidingWindowRateLimiterOptions);
        // Saves an allocation in GetSlidingWindowLimiter, which would have created a new set of options if this was true.
        slidingWindowRateLimiterOptions.AutoReplenishment = false;
        return options.AddPolicy(policyName, context =>
        {
            return RateLimitPartition.GetSlidingWindowLimiter(key,
                _ => slidingWindowRateLimiterOptions);
        });
    }

    /// <summary>
    /// Adds a new <see cref="ConcurrencyLimiter"/> with the given <see cref="ConcurrencyLimiterOptions"/> to the <see cref="RateLimiterOptions"/>.
    /// </summary>
    /// <param name="options">The <see cref="RateLimiterOptions"/> to add a limiter to.</param>
    /// <param name="policyName">The name that will be associated with the limiter.</param>
    /// <param name="configureOptions">A callback to configure the <see cref="ConcurrencyLimiterOptions"/> to be used for the limiter.</param>
    /// <returns>This <see cref="RateLimiterOptions"/>.</returns>
    public static RateLimiterOptions AddConcurrencyLimiter(this RateLimiterOptions options, string policyName, Action<ConcurrencyLimiterOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        var key = new PolicyNameKey() { PolicyName = policyName };
        var concurrencyLimiterOptions = new ConcurrencyLimiterOptions();
        configureOptions.Invoke(concurrencyLimiterOptions);
        return options.AddPolicy(policyName, context =>
        {
            return RateLimitPartition.GetConcurrencyLimiter(key,
                _ => concurrencyLimiterOptions);
        });
    }
}
