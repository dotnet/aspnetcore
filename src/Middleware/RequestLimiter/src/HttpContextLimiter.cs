using System.Threading;
using System.Threading.ResourceLimits;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RequestLimiter
{
    internal class HttpContextLimiter : AggregatedResourceLimiter<HttpContext> 
    {
        private readonly ResourceLimiter _limiter;

        public HttpContextLimiter(ResourceLimiter limiter)
        {
            _limiter = limiter;
        }

        public override ResourceLease Acquire(HttpContext resourceID, long requestedCount)
        {
            return _limiter.Acquire(requestedCount);
        }

        public override long EstimatedCount(HttpContext resourceID)
        {
            return _limiter.EstimatedCount;
        }

        public override ValueTask<ResourceLease> WaitAsync(HttpContext resourceID, long requestedCount, CancellationToken cancellationToken = default)
        {
            return _limiter.WaitAsync(requestedCount, cancellationToken);
        }

        public static implicit operator HttpContextLimiter(ResourceLimiter limiter) => new HttpContextLimiter(limiter);
    }
}
