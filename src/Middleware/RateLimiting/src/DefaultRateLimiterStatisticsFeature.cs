// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RateLimiting;

internal sealed class DefaultRateLimiterStatisticsFeature : IRateLimiterStatisticsFeature
{
    private readonly PartitionedRateLimiter<HttpContext>? _globalLimiter;
    private readonly PartitionedRateLimiter<HttpContext> _endpointLimiter;

    internal HttpContext? HttpContext { private get; set; }

    public DefaultRateLimiterStatisticsFeature(
        PartitionedRateLimiter<HttpContext>? globalLimiter,
        PartitionedRateLimiter<HttpContext> endpointLimiter)
    {
        _globalLimiter = globalLimiter;
        _endpointLimiter = endpointLimiter;
    }

    public RateLimiterStatistics? GetEndpointStatistics() => _endpointLimiter.GetStatistics(HttpContext);

    public RateLimiterStatistics? GetGlobalStatistics() => _globalLimiter?.GetStatistics(HttpContext);
}
