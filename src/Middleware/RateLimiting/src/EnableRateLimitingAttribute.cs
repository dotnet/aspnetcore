// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Metadata that provides endpoint-specific request rate limiting.
/// </summary>
/// <remarks>
/// Replaces any policies currently applied to the endpoint.
/// The global limiter will still run on endpoints with this attribute applied.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class EnableRateLimitingAttribute : Attribute
{
    /// <summary>
    /// Creates a new instance of <see cref="EnableRateLimitingAttribute"/> using the specified policy.
    /// </summary>
    /// <param name="policyName">The name of the policy which needs to be applied.</param>
    public EnableRateLimitingAttribute(string policyName)
    {
        ArgumentNullException.ThrowIfNull(policyName);

        PolicyName = policyName;
    }

    internal EnableRateLimitingAttribute(DefaultRateLimiterPolicy policy)
    {
        Policy = policy;
    }

    /// <summary>
    /// The name of the policy which needs to be applied.
    /// </summary>
    public string? PolicyName { get; }

    /// <summary>
    /// The policy which needs to be applied, if present.
    /// </summary>
    internal DefaultRateLimiterPolicy? Policy { get; }
}
