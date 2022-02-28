// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.RateLimiting.Policies;

/// <summary>
/// Metadata that provides a Rate Limiting policy.
/// </summary>
public class RateLimitingPolicyMetadata : IRateLimitingPolicyMetadata
{

    /// <summary>
    /// Creates a new instance of <see cref="RateLimitingPolicyMetadata"/> using the specified policy.
    /// </summary>
    /// <param name="name">The name of the policy which needs to be applied.</param>
    public RateLimitingPolicyMetadata(string name)
    {
        Name = name;
    }

    /// <summary>
    /// The name of the policy which needs to be applied.
    /// </summary>
    public string Name { get; }
}
