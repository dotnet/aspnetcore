// Pending aspnetcore API review

using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.RateLimits
{
    // Represent an aggregated resource (e.g. a resource limiter aggregated by IP)
    public abstract class AggregatedRateLimiter<TKey>
    {
        // an inaccurate view of resources
        public abstract int AvailablePermits(TKey resourceID);

        // Fast synchronous attempt to acquire resources
        public abstract PermitLease Acquire(TKey resourceID, int requestedCount);

        // Wait until the requested resources are available
        public abstract ValueTask<PermitLease> WaitAsync(TKey resourceID, int requestedCount, CancellationToken cancellationToken = default);
    }
}
