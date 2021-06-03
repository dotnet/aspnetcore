using System;
using System.Threading;
using System.Runtime.RateLimits;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RequestLimiter
{
    internal class AggregatedLimiterWrapper<TContext> : AggregatedRateLimiter<HttpContext> where TContext: notnull
    {
        private readonly AggregatedRateLimiter<TContext> _limiter;
        private readonly Func<HttpContext, TContext> _selector;

        public AggregatedLimiterWrapper(AggregatedRateLimiter<TContext> limiter, Func<HttpContext, TContext> selector)
        {
            _limiter = limiter;
            _selector = selector;
        }

        public override PermitLease Acquire(HttpContext context, int permitCount)
        {
            return _limiter.Acquire(_selector(context), permitCount);
        }

        public override int AvailablePermits(HttpContext context)
        {
            return _limiter.AvailablePermits(_selector(context));
        }

        public override ValueTask<PermitLease> WaitAsync(HttpContext context, int requestedCount, CancellationToken cancellationToken = default)
        {
            return _limiter.WaitAsync(_selector(context), requestedCount, cancellationToken);
        }
    }
}
