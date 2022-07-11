// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Metadata that provides endpoint-specific request rate limiting.
/// </summary>
internal sealed class RateLimiterMetadata : IRateLimiterMetadata
{
    /// <summary>
    /// Creates a new instance of <see cref="RateLimiterMetadata"/> using the specified policy.
    /// </summary>
    /// <param name="policyName">The name of the policy which needs to be applied.</param>
    public RateLimiterMetadata(string policyName)
    {
        PolicyName = policyName;
    }

    /// <summary>
    /// The name of the policy which needs to be applied.
    /// </summary>
    public string PolicyName { get; }
}
