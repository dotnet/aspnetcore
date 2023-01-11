// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RateLimiting.Features;

/// <summary>
/// An Interface which is used to represent <see cref="RateLimiter"/> statistics methods for global and endpoint limiters.
/// Obtained via <see cref="HttpContext.Features"/>.
/// </summary>
/// <remarks>
/// Requires <see cref="RateLimiterOptions.TrackStatistics"/> to be true
/// </remarks>
public interface IRateLimiterStatisticsFeature
{
    /// <summary>
    /// Method to fetch <see cref="RateLimiterStatistics"/> for global <see cref="RateLimiter"/>
    /// </summary>
    /// <returns><see cref="RateLimiterStatistics"/> for global <see cref="RateLimiter"/>.</returns>
    RateLimiterStatistics? GetGlobalStatistics();
    /// <summary>
    /// Method to fetch <see cref="RateLimiterStatistics"/> for global <see cref="RateLimiter"/>
    /// </summary>
    /// <returns><see cref="RateLimiterStatistics"/> for global <see cref="RateLimiter"/>.</returns>
    RateLimiterStatistics? GetEndpointStatistics();
}
