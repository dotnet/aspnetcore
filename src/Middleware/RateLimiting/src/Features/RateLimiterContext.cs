// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RateLimiting.Features;

public class RateLimiterContext
{
    public required HttpContext HttpContext { get; set; }

    public required RateLimitLease Lease { get; set; }

    public required PartitionedRateLimiter<HttpContext>? GlobalLimiter { get; set; }

    public required PartitionedRateLimiter<HttpContext> EndpointLimiter { get; set; }
}
