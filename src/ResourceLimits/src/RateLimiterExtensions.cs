// Will be migrated to dotnet/runtime
// Pending dotnet API review

using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.RateLimits
{
    public static class RateLimiterExtensions
    {
        public static PermitLease Acquire(this RateLimiter limiter)
        {
            return limiter.Acquire(1);
        }

        public static ValueTask<PermitLease> WaitAsync(this RateLimiter limiter, CancellationToken cancellationToken = default)
        {
            return limiter.WaitAsync(1, cancellationToken);
        }
        public static PermitLease Acquire<TKey>(this AggregatedRateLimiter<TKey> limiter, TKey resourceId)
        {
            return limiter.Acquire(resourceId, 1);
        }

        public static ValueTask<PermitLease> WaitAsync<TKey>(this AggregatedRateLimiter<TKey> limiter, TKey resourceId, CancellationToken cancellationToken = default)
        {
            return limiter.WaitAsync(resourceId, 1, cancellationToken);
        }
    }
}
