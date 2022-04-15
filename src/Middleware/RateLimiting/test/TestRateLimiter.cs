// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;

namespace Microsoft.AspNetCore.RateLimiting;
internal class TestRateLimiter : RateLimiter
{
    private readonly bool _alwaysAccept;

    public TestRateLimiter(bool alwaysAccept)
    {
        _alwaysAccept = alwaysAccept;
    }

    public override TimeSpan? IdleDuration => throw new NotImplementedException();

    public override int GetAvailablePermits()
    {
        throw new NotImplementedException();
    }

    protected override RateLimitLease AcquireCore(int permitCount)
    {
        return new TestRateLimitLease(_alwaysAccept, null);
    }

    protected override ValueTask<RateLimitLease> WaitAsyncCore(int permitCount, CancellationToken cancellationToken)
    {
        return new ValueTask<RateLimitLease>(new TestRateLimitLease(_alwaysAccept, null));
    }
}
