// Pending API review

using System.Threading.Tasks;

namespace System.Threading.ResourceLimits
{
    // Represent an aggregated resource (e.g. a resource limiter aggregated by IP)
    public abstract class AggregatedResourceLimiter<TKey>
    {
        // an inaccurate view of resources
        public abstract long EstimatedCount(TKey resourceID);

        // Fast synchronous attempt to acquire resources
        public abstract Resource Acquire(TKey resourceID, long requestedCount);

        // Wait until the requested resources are available
        // If unsuccessful, throw
        public abstract ValueTask<Resource> AcquireAsync(TKey resourceID, long requestedCount, CancellationToken cancellationToken = default);
    }
}
