// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.RateLimiting;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Rate limiter extension methods for <see cref="IEndpointConventionBuilder"/>.
/// </summary>
public static class RateLimiterEndpointConventionBuilderExtensions
{
    /// <summary>
    /// Adds the specified rate limiting policy to the endpoint(s).
    /// </summary>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <param name="policyName">The name of the rate limiting policy to add to the endpoint.</param>
    /// <returns>The original convention builder parameter.</returns>
    public static TBuilder RequireRateLimiting<TBuilder>(this TBuilder builder, string policyName) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(policyName);

        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(new EnableRateLimitingAttribute(policyName));
        });

        return builder;
    }

    /// <summary>
    /// Adds the specified rate limiting policy to the endpoint(s).
    /// </summary>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <param name="policy">The rate limiting policy to add to the endpoint.</param>
    /// <returns>The original convention builder parameter.</returns>
    public static TBuilder RequireRateLimiting<TBuilder, TPartitionKey>(this TBuilder builder, IRateLimiterPolicy<TPartitionKey> policy) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(policy);

        // Inline policies are not named, so historically they all shared a single null partition-key
        // namespace. That let two distinct inline policies collide onto the same limiter whenever their
        // GetPartition methods returned equal keys. Derive a stable namespace from the policy instance so
        // distinct policy objects are isolated while the same instance reused across endpoints still shares
        // a limiter. This is captured once here rather than inside the convention callback so endpoint
        // rebuilds don't regenerate it. It is intentionally kept separate from the telemetry-facing
        // EnableRateLimitingAttribute.PolicyName so rate-limiting metrics are unaffected.
        var policyName = $"__inlinePolicy_{RuntimeHelpers.GetHashCode(policy)}";

        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(new EnableRateLimitingAttribute(new DefaultRateLimiterPolicy(RateLimiterOptions.ConvertPartitioner<TPartitionKey>(policyName, policy.GetPartition), policy.OnRejected)));
        });
        return builder;
    }

    /// <summary>
    /// Disables rate limiting on the endpoint(s).
    /// </summary>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <returns>The original convention builder parameter.</returns>
    /// <remarks>Will skip both the global limiter, and any endpoint-specific limiters that apply to the endpoint(s).</remarks>
    public static TBuilder DisableRateLimiting<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(DisableRateLimitingAttribute.Instance);
        });

        return builder;
    }
}
