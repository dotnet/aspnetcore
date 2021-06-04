// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.RateLimits;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.RequestLimiter
{
    public class RequestLimiterOptions
    {
        internal Dictionary<string, RequestLimiterPolicy> PolicyMap { get; } = new Dictionary<string, RequestLimiterPolicy>(StringComparer.OrdinalIgnoreCase);

        internal Func<IServiceProvider, AggregatedRateLimiter<HttpContext>>? ResolveDefaultRequestLimit { get; set; }

        public void SetDefaultPolicy(RateLimiter limiter)
        {
            ResolveDefaultRequestLimit = _ => new SimpleLimiterWrapper(limiter);
        }

        public void SetDefaultPolicy<TContext>(AggregatedRateLimiter<TContext> aggregatedLimiter, Func<HttpContext, TContext> selector) where TContext : notnull
        {
            ResolveDefaultRequestLimit = _ => new AggregatedLimiterWrapper<TContext>(aggregatedLimiter, selector);
        }
        public void SetDefaultPolicy<TRateLimiter>() where TRateLimiter : RateLimiter
        {
            ResolveDefaultRequestLimit = services => new SimpleLimiterWrapper(services.GetRequiredService<TRateLimiter>());
        }

        public void SetDefaultPolicy<TRateLimiter, TContext>(Func<HttpContext, TContext> selector) where TRateLimiter : AggregatedRateLimiter<TContext> where TContext : notnull
        {
            ResolveDefaultRequestLimit = services => new AggregatedLimiterWrapper<TContext>(services.GetRequiredService<TRateLimiter>(), selector);
        }

        public void AddPolicy(string name, Action<RequestLimiterPolicy> configurePolicy)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var policy = new RequestLimiterPolicy();
            configurePolicy(policy);

            PolicyMap[name] = policy;
        }

        public Func<HttpContext, PermitLease, Task> OnRejected { get; set; } = (context, permitLease) => Task.CompletedTask;
    }
}
