// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;

namespace Microsoft.AspNetCore.RateLimiting;

internal struct LeaseContext : IDisposable
{
    public RequestRejectionReason? RequestRejectionReason { get; init; }

    public RateLimitLease? Lease { get; init; }

    public void Dispose()
    {
        Lease?.Dispose();
    }
}

internal enum RequestRejectionReason
{
    EndpointLimiter,
    GlobalLimiter,
    RequestCanceled
}
