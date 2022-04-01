// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;

namespace Microsoft.AspNetCore.RateLimiting;
internal class NoLimiter<TResource> : PartitionedRateLimiter<TResource>
{
    public override int GetAvailablePermits(TResource resourceID)
    {
        return 1;
    }

    protected override RateLimitLease AcquireCore(TResource resourceID, int permitCount)
    {
        return new NoLimiterLease();
    }

    protected override ValueTask<RateLimitLease> WaitAsyncCore(TResource resourceID, int permitCount, CancellationToken cancellationToken)
    {
        return new ValueTask<RateLimitLease>(new NoLimiterLease());
    }
}

internal class NoLimiterLease : RateLimitLease
{
    public override bool IsAcquired => true;

    public override IEnumerable<string> MetadataNames => new List<string>();

    public override bool TryGetMetadata(string metadataName, out object? metadata)
    {
        metadata = null;
        return false;
    }
}
