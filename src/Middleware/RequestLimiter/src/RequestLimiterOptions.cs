// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.ResourceLimits;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RequestLimiter
{
    /// <summary>
    /// 
    /// </summary>
    public class RequestLimiterOptions
    {
        internal Dictionary<string, RequestLimiterPolicy> PolicyMap { get; } = new Dictionary<string, RequestLimiterPolicy>(StringComparer.OrdinalIgnoreCase);

        internal RequestLimitRegistration? DefaultRequestLimitRegistration { get; set; }

        public void SetDefaultPolicy(RequestLimitRegistration policyRegistration)
        {
            DefaultRequestLimitRegistration = policyRegistration;
        }

        public void SetDefaultPolicy(ResourceLimiter limiter)
        {
            DefaultRequestLimitRegistration = new RequestLimitRegistration(limiter);
        }

        public void SetDefaultPolicy(AggregatedResourceLimiter<HttpContext> aggregatedLimiter)
        {
            DefaultRequestLimitRegistration = new RequestLimitRegistration(aggregatedLimiter);
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
    }
}
