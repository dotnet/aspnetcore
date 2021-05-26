// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.ResourceLimits;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RequestLimiter
{
    public static class RequestLimiterEndpointExtensions
    {
        public static IEndpointConventionBuilder EnforceLimit(this IEndpointConventionBuilder builder)
        {
            builder.Add(endpointBuilder =>
            {
                endpointBuilder.Metadata.Add(new RequestLimitAttribute());
            });

            return builder;
        }

        public static IEndpointConventionBuilder EnforceLimit(this IEndpointConventionBuilder builder, string policyName)
        {
            builder.Add(endpointBuilder =>
            {
                endpointBuilder.Metadata.Add(new RequestLimitAttribute(policyName));
            });

            return builder;
        }

        public static IEndpointConventionBuilder EnforceRateLimit(this IEndpointConventionBuilder builder, long requestPerSecond)
        {
            builder.Add(endpointBuilder =>
            {
                endpointBuilder.Metadata.Add(
                    new RequestLimitAttribute(
                        (HttpContextLimiter)new TokenBucketRateLimiter(
                            requestPerSecond,
                            requestPerSecond)));
            });

            return builder;
        }

        public static IEndpointConventionBuilder EnforceConcurrencyLimit(this IEndpointConventionBuilder builder, long concurrentRequests)
        {
            builder.Add(endpointBuilder =>
            {
                endpointBuilder.Metadata.Add(
                    new RequestLimitAttribute(
                        (HttpContextLimiter)new ConcurrencyLimiter(
                            new ConcurrencyLimiterOptions { ResourceLimit = concurrentRequests })));
            });

            return builder;
        }

        public static IEndpointConventionBuilder EnforceLimit(this IEndpointConventionBuilder builder, ResourceLimiter limiter)
            => builder.EnforceLimit((HttpContextLimiter)limiter);

        public static IEndpointConventionBuilder EnforceLimit(this IEndpointConventionBuilder builder, AggregatedResourceLimiter<HttpContext> limiter)
        {

            builder.Add(endpointBuilder =>
            {
                endpointBuilder.Metadata.Add(new RequestLimitAttribute(limiter));
            });

            return builder;
        }
    }
}
