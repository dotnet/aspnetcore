// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.ResourceLimits;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.RequestLimiter
{
    /// <summary>
    /// 
    /// </summary>
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

        public static IEndpointConventionBuilder EnforceLimit(this IEndpointConventionBuilder builder, long requestPerSecond)
        {

            builder.Add(endpointBuilder =>
            {
                endpointBuilder.Metadata.Add(new RequestLimitAttribute(requestPerSecond));
            });

            return builder;
        }

        public static IEndpointConventionBuilder EnforceLimit(this IEndpointConventionBuilder builder, RateLimiter limiter)
        {

            builder.Add(endpointBuilder =>
            {
                endpointBuilder.Metadata.Add(new RequestLimitAttribute(limiter));
            });

            return builder;
        }

        public static IEndpointConventionBuilder EnforceLimit<TResourceLimiter>(this IEndpointConventionBuilder builder)
            where TResourceLimiter : ResourceLimiter
        {
            builder.Add(endpointBuilder =>
            {
                endpointBuilder.Metadata.Add(new RequestLimitAttribute(new RequestLimitRegistration(services => services.GetRequiredService<TResourceLimiter>())));
            });
            return builder;
        }

        public static IEndpointConventionBuilder EnforceAggregatedLimit<TAggregatedResourceLimiter>(this IEndpointConventionBuilder builder)
            where TAggregatedResourceLimiter : AggregatedResourceLimiter<HttpContext>
        {
            builder.Add(endpointBuilder =>
            {
                endpointBuilder.Metadata.Add(new RequestLimitAttribute(new RequestLimitRegistration(services => services.GetRequiredService<TAggregatedResourceLimiter>())));
            });
            return builder;
        }
    }
}
