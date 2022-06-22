// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RateLimiting;
internal sealed class AspNetPolicy : IRateLimiterPolicy<AspNetKey>
{
    private readonly Func<HttpContext, RateLimitPartition<AspNetKey>> _partitioner;

    public AspNetPolicy(Func<HttpContext, RateLimitPartition<AspNetKey>> partitioner)
    {
        _partitioner = partitioner;
    }

    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => null;

    public RateLimitPartition<AspNetKey> GetPartition(HttpContext httpContext)
    {
        return _partitioner(httpContext);
    }
}
