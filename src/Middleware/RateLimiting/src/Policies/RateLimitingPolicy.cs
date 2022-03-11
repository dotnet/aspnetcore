// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RateLimiting.Policies;
internal class RateLimitingPolicy
{
    private readonly RateLimiter _limiter;
    //private readonly RateLimiter<HttpContext> _limiterOfT;

    public RateLimitingPolicy(RateLimiter limiter)
    {
        if (limiter == null)
        {
            throw new ArgumentNullException(nameof(limiter));
        }
        _limiter = limiter;
    }

    /*
    public RateLimitingPolicy(RateLimiter<HttpContext> limiter)
    {
        //Error handling()
        _limiter = limiter;
    }
    */
}
