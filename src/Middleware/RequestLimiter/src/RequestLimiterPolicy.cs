// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.ResourceLimits;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.RequestLimiter
{
    public class RequestLimiterPolicy
    {
        internal ICollection<Func<IServiceProvider, AggregatedResourceLimiter<HttpContext>>> LimiterResolvers { get; } = new List<Func<IServiceProvider, AggregatedResourceLimiter<HttpContext>>>();

        public void AddLimiter(RateLimiter limiter)
        {
            LimiterResolvers.Add(_ => (HttpContextLimiter)limiter);
        }

        public void AddAggregatedLimiter(AggregatedResourceLimiter<HttpContext> aggregatedLimiter)
        {
            LimiterResolvers.Add(_ => aggregatedLimiter);
        }

        public void AddLimiter<TResourceLimiter>() where TResourceLimiter : RateLimiter
        {
            LimiterResolvers.Add(services => (HttpContextLimiter)services.GetRequiredService<TResourceLimiter>());
        }

        // TODO: non aggregated limiters
        public void AddAggregatedLimiter<TResourceAggregatedLimiter>() where TResourceAggregatedLimiter : AggregatedResourceLimiter<HttpContext>
        {
            LimiterResolvers.Add(services => services.GetRequiredService<TResourceAggregatedLimiter>());
        }
    }
}
