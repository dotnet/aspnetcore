// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// An interface which is used to represent a RateLimiter policy.
/// </summary>
public interface IRateLimiterPolicy<TPartitionKey>
{
    /// <summary>
    /// Gets the <see cref="Func{OnRejectedContext, CancellationToken, ValueTask}"/> that handles requests rejected by this middleware.
    /// </summary>
    Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected { get; }

    /// <summary>
    /// Gets the <see cref="RateLimitPartition{TPartitionKey}"/> that applies to the given <see cref="HttpContext"/>.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> to get the partition for.</param>
    RateLimitPartition<TPartitionKey> GetPartition(HttpContext httpContext);
}
