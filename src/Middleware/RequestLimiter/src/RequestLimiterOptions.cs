// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.ResourceLimits;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.RequestLimiter
{
    public class RequestLimiterOptions
    {
        internal Dictionary<string, RequestLimiterPolicy> PolicyMap { get; } = new Dictionary<string, RequestLimiterPolicy>(StringComparer.OrdinalIgnoreCase);

        internal Func<IServiceProvider, AggregatedResourceLimiter<HttpContext>>? ResolveDefaultRequestLimit { get; set; }

        public void SetDefaultPolicy(ResourceLimiter limiter)
        {
            ResolveDefaultRequestLimit = _ => (HttpContextLimiter)limiter;
        }

        public void SetDefaultPolicy(AggregatedResourceLimiter<HttpContext> aggregatedLimiter)
        {
            ResolveDefaultRequestLimit = _ => aggregatedLimiter;
        }

        public void SetDefaultPolicy<TResourceLimiter>() where TResourceLimiter : AggregatedResourceLimiter<HttpContext>
        {
            ResolveDefaultRequestLimit = services => services.GetRequiredService<TResourceLimiter>();
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

        public Func<HttpContext, ResourceLease, Task> OnRejected { get; set; } = (context, resourceLease) => Task.CompletedTask;
    }
}
