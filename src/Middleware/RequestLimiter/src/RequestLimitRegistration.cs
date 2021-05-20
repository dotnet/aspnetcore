using System;
using System.Threading.ResourceLimits;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RequestLimiter
{
    public class RequestLimitRegistration
    {
        internal Func<IServiceProvider, ResourceLimiter>? ResolveLimiter { get; }
        internal Func<IServiceProvider, AggregatedResourceLimiter<HttpContext>>? ResolveAggregatedLimiter { get; }

        public RequestLimitRegistration(ResourceLimiter limiter)
        {
            ResolveLimiter = _ => limiter;
        }

        public RequestLimitRegistration(AggregatedResourceLimiter<HttpContext> aggregatedLimiter)
        {
            ResolveAggregatedLimiter = _ => aggregatedLimiter;
        }

        public RequestLimitRegistration(Func<IServiceProvider, ResourceLimiter> resolveLimiter)
        {
            ResolveLimiter = resolveLimiter;
        }

        public RequestLimitRegistration(Func<IServiceProvider, AggregatedResourceLimiter<HttpContext>> resolveAggregatedLimiter)
        {
            ResolveAggregatedLimiter = resolveAggregatedLimiter;
        }
    }
}
