// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RateLimiting.Features;

internal class DefaultRateLimiterStatisticsFeature : IRateLimiterStatisticsFeature
{
    private readonly HttpContext _httpContext;
    private readonly PartitionedRateLimiter<HttpContext>? _globalLimiter;
    private readonly PartitionedRateLimiter<HttpContext> _endpointLimiter;

    public DefaultRateLimiterStatisticsFeature(
        PartitionedRateLimiter<HttpContext>? globalLimiter,
        PartitionedRateLimiter<HttpContext> endpointLimiter,
        HttpContext httpContext)
    {
        _globalLimiter = globalLimiter;
        _endpointLimiter = endpointLimiter;
        _httpContext = httpContext;
    }

    public RateLimiterStatistics? GetEndpointStatistics() => _endpointLimiter.GetStatistics(_httpContext);

    public RateLimiterStatistics? GetGlobalStatistics() => _globalLimiter?.GetStatistics(_httpContext);
}
