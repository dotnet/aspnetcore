// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.ResourceLimits;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RequestLimiter
{
    /// <summary>
    /// Specifies that the class or method that this attribute is applied to requires the specified authorization.
    /// </summary>
    // TODO: Double check ordering
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RequestLimitAttribute : Attribute
    {
        public RequestLimitAttribute() { }

        public RequestLimitAttribute(string policy)
        {
            Policy = policy;
        }

        public RequestLimitAttribute(long requestPerSecond)
            : this(new RateLimiter(requestPerSecond, requestPerSecond))
        { }

        public RequestLimitAttribute(ResourceLimiter limiter)
        {
            LimiterRegistration = new RequestLimitRegistration(limiter);
        }

        public RequestLimitAttribute(AggregatedResourceLimiter<HttpContext> limiter)
        {
            LimiterRegistration = new RequestLimitRegistration(limiter);
        }

        // TODO consider constructors that take in types for DI retrieval
        public RequestLimitAttribute(RequestLimitRegistration registration)
        {
            LimiterRegistration = registration;
        }

        public string? Policy { get; set; }

        public RequestLimitRegistration? LimiterRegistration { get; set; }
    }
}
