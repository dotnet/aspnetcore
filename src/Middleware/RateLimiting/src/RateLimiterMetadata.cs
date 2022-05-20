// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Metadata that provides endpoint-specific request rate limiting.
/// </summary>
public class RateLimiterMetadata : IRateLimiterMetadata
{
    /// <summary>
    /// Creates a new instance of <see cref="RateLimiterMetadata"/> using the specified limiter.
    /// </summary>
    /// <param name="name">The name of the limiter which needs to be applied.</param>
    public RateLimiterMetadata(string name)
    {
        Name = name;
    }

    /// <summary>
    /// The name of the limiter which needs to be applied.
    /// </summary>
    public string Name { get; }
}
