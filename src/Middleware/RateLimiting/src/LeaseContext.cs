// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;

namespace Microsoft.AspNetCore.RateLimiting;
internal sealed class LeaseContext : IDisposable
{
    public Rejector? Rejector { get; init; }

    public required RateLimitLease Lease { get; init; }

    public void Dispose()
    {
        Lease.Dispose();
    }
}
