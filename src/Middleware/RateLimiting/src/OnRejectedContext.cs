// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RateLimiting;
public sealed class OnRejectedContext
{
    public required HttpContext HttpContext { get; init; }

    public required RateLimitLease Lease { get; init; }
}
