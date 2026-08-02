// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RateLimiting;

internal sealed class DefaultRateLimiterPolicy : IRateLimiterPolicy<DefaultKeyType>
{
    private readonly Func<HttpContext, RateLimitPartition<DefaultKeyType>> _partitioner;
    private readonly Func<OnRejectedContext, CancellationToken, ValueTask>? _onRejected;

    public DefaultRateLimiterPolicy(Func<HttpContext, RateLimitPartition<DefaultKeyType>> partitioner, Func<OnRejectedContext, CancellationToken, ValueTask>? onRejected)
    {
        _partitioner = partitioner;
        _onRejected = onRejected;
    }

    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => _onRejected;

    public RateLimitPartition<DefaultKeyType> GetPartition(HttpContext httpContext)
    {
        return _partitioner(httpContext);
    }
}
