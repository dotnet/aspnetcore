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
            LimiterResolvers.Add(_ => new SimpleLimiterWrapper(limiter));
        }

        public void AddAggregatedLimiter<TContext>(AggregatedRateLimiter<TContext> aggregatedLimiter, Func<HttpContext, TContext> selector) where TContext : notnull
        {
            LimiterResolvers.Add(_ => new AggregatedLimiterWrapper<TContext>(aggregatedLimiter, selector));
        }

        public void AddLimiter<TRateLimiter>() where TRateLimiter : RateLimiter
        {
            LimiterResolvers.Add(services => new SimpleLimiterWrapper(services.GetRequiredService<TRateLimiter>()));
        }

        public void AddAggregatedLimiter<TAggregatedRateLimiter, TContext>(Func<HttpContext, TContext> selector) where TAggregatedRateLimiter : AggregatedRateLimiter<TContext> where TContext : notnull
        {
            LimiterResolvers.Add(services => new AggregatedLimiterWrapper<TContext>(services.GetRequiredService<TAggregatedRateLimiter>(), selector));
        }
    }
}
