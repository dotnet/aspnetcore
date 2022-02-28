// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.RateLimiting;
public static class RateLimitingOptionsExtensions
{
    public static RateLimitingOptions AddTokenBucketRateLimiter(this RateLimitingOptions options, string name, TokenBucketRateLimiterOptions tokenBucketRateLimiterOptions)
    {
        return options.AddLimiter(name, new TokenBucketRateLimiter(tokenBucketRateLimiterOptions));
    }

    public static RateLimitingOptions AddConcurrencyLimiter(this RateLimitingOptions options, string name, ConcurrencyLimiterOptions concurrencyLimiterOptions)
    {
        return options.AddLimiter(name, new ConcurrencyLimiter(concurrencyLimiterOptions));
    }
}
