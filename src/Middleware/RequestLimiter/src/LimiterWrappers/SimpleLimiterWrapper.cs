using System.Threading;
using System.Runtime.RateLimits;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RequestLimiter
{
    internal class SimpleLimiterWrapper : AggregatedRateLimiter<HttpContext>
    {
        private readonly RateLimiter _limiter;

        public SimpleLimiterWrapper(RateLimiter limiter)
        {
            _limiter = limiter;
        }

        public override PermitLease Acquire(HttpContext context, int permitCount)
        {
            return _limiter.Acquire(permitCount);
        }

        public override int AvailablePermits(HttpContext context)
        {
            return _limiter.AvailablePermits;
        }

        public override ValueTask<PermitLease> WaitAsync(HttpContext context, int requestedCount, CancellationToken cancellationToken = default)
        {
            return _limiter.WaitAsync(requestedCount, cancellationToken);
        }
    }
}
