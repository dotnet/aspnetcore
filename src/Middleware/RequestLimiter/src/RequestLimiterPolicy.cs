// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.ResourceLimits;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.RequestLimiter
{
    public class RequestLimiterPolicy
    {
        public ICollection<RequestLimitRegistration> Limiters { get; } = new List<RequestLimitRegistration>();

        public void AddLimiter(RequestLimitRegistration registration)
        {
            Limiters.Add(registration);
        }

        public void AddLimiter(ResourceLimiter limiter)
        {
            Limiters.Add(new RequestLimitRegistration(limiter));
        }

        public void AddLimiter(AggregatedResourceLimiter<HttpContext> aggregatedLimiter)
        {
            Limiters.Add(new RequestLimitRegistration(aggregatedLimiter));
        }

        public void AddLimiter<TResourceLimiter>() where TResourceLimiter : ResourceLimiter
        {
            Limiters.Add(new RequestLimitRegistration(services => services.GetRequiredService<TResourceLimiter>()));
        }
    }
}
