// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RateLimiting;

public interface IRateLimiterPolicy<TKey>
{
    public int CustomRejectionStatusCode { get; }

    public RateLimitPartition<TKey> GetPartition(HttpContext httpContext);
}
