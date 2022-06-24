// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RateLimiting;
internal sealed class AspNetPolicy : IRateLimiterPolicy<AspNetKey>
{
    private readonly Func<HttpContext, RateLimitPartition<AspNetKey>> _partitioner;
    private readonly Func<OnRejectedContext, CancellationToken, ValueTask>? _onRejected;

    public AspNetPolicy(Func<HttpContext, RateLimitPartition<AspNetKey>> partitioner, Func<OnRejectedContext, CancellationToken, ValueTask>? onRejected)
    {
        _partitioner = partitioner;
        _onRejected = onRejected;
    }

    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => _onRejected;

    public RateLimitPartition<AspNetKey> GetPartition(HttpContext httpContext)
    {
        return _partitioner(httpContext);
    }
}
