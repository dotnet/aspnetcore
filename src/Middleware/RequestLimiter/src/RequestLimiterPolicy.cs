// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.RateLimits;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.RequestLimiter
{
    public class RequestLimiterPolicy
    {
        internal ICollection<Func<IServiceProvider, AggregatedRateLimiter<HttpContext>>> LimiterResolvers { get; } = new List<Func<IServiceProvider, AggregatedRateLimiter<HttpContext>>>();

        public void AddLimiter(RateLimiter limiter)
        {
            LimiterResolvers.Add(_ => (HttpContextLimiter)limiter);
        }

        public void AddAggregatedLimiter(AggregatedRateLimiter<HttpContext> aggregatedLimiter)
        {
            LimiterResolvers.Add(_ => aggregatedLimiter);
        }

        public void AddLimiter<TRateLimiter>() where TRateLimiter : RateLimiter
        {
            LimiterResolvers.Add(services => (HttpContextLimiter)services.GetRequiredService<TRateLimiter>());
        }

        public void AddAggregatedLimiter<TAggregatedRateLimiter>() where TAggregatedRateLimiter : AggregatedRateLimiter<HttpContext>
        {
            LimiterResolvers.Add(services => services.GetRequiredService<TAggregatedRateLimiter>());
        }
    }
}
