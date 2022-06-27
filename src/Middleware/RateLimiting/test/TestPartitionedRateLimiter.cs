// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.RateLimiting;

internal class TestPartitionedRateLimiter<TResource> : PartitionedRateLimiter<TResource>
{
    private List<RateLimiter> limiters = new List<RateLimiter>();

    public TestPartitionedRateLimiter() { }

    public TestPartitionedRateLimiter(RateLimiter limiter)
    {
        limiters.Add(limiter);
    }

    public void AddLimiter(RateLimiter limiter)
    {
        limiters.Add(limiter);
    }

    public override int GetAvailablePermits(TResource resourceID)
    {
        throw new NotImplementedException();
    }

    protected override RateLimitLease AcquireCore(TResource resourceID, int permitCount)
    {
        if (permitCount != 1)
        {
            throw new ArgumentException("Tests only support 1 permit at a time");
        }    
        var leases = new List<RateLimitLease>();
        foreach (var limiter in limiters)
        {
            var lease = limiter.Acquire();
            if (lease.IsAcquired)
            {
                leases.Add(lease);
            }
            else
            {
                foreach (var unusedLease in leases)
                {
                    unusedLease.Dispose();
                }
                return new TestRateLimitLease(false, null);
            }
        }
        return new TestRateLimitLease(true, leases);
    }

    protected override async ValueTask<RateLimitLease> WaitAsyncCore(TResource resourceID, int permitCount, CancellationToken cancellationToken)
    {
        if (permitCount != 1)
        {
            throw new ArgumentException("Tests only support 1 permit at a time");
        }
        var leases = new List<RateLimitLease>();
        foreach (var limiter in limiters)
        {
            leases.Add(await limiter.WaitAsync().ConfigureAwait(false));
        }
        foreach (var lease in leases)
        {
            if (!lease.IsAcquired)
            {
                foreach (var unusedLease in leases)
                {
                    unusedLease.Dispose();
                }
                return new TestRateLimitLease(false, null);
            }    
        }
        return new TestRateLimitLease(true, leases);

    }
}
