// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;

namespace Microsoft.AspNetCore.RateLimiting;

internal class TestRateLimitLease : RateLimitLease
{
    internal List<RateLimitLease> _leases;

    public TestRateLimitLease(bool isAcquired, List<RateLimitLease> leases)
    {
        IsAcquired = isAcquired;
        _leases = leases;
    }

    public override bool IsAcquired { get; }

    public override IEnumerable<string> MetadataNames => throw new NotImplementedException();

    public override bool TryGetMetadata(string metadataName, out object metadata)
    {
        throw new NotImplementedException();
    }

    protected override void Dispose(bool disposing)
    {
        if (_leases != null)
        {
            foreach (var lease in _leases)
            {
                lease.Dispose();
            }
        }
    }
}
