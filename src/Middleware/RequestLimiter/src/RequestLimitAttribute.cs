// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.ResourceLimits;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RequestLimiter
{
    // TODO: Double check ordering
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RequestLimitAttribute : Attribute
    {
        public RequestLimitAttribute() { }

        public RequestLimitAttribute(string policy)
        {
            Policy = policy;
        }

        public RequestLimitAttribute(AggregatedResourceLimiter<HttpContext> limiter)
        {
            Limiter = limiter;
        }

        internal string? Policy { get; }

        internal AggregatedResourceLimiter<HttpContext>? Limiter { get; }
    }
}
