// Pending API review

using System.Threading.Tasks;

namespace System.Threading.ResourceLimits
{
    public abstract class ResourceLimiter
    {
        // an inaccurate view of resources
        public abstract long EstimatedCount { get; }

        // Fast synchronous attempt to acquire resources
        public abstract Resource Acquire(long requestedCount);

        // Wait until the requested resources are available
        // If unsuccessful, throw
        public abstract ValueTask<Resource> AcquireAsync(long requestedCount, CancellationToken cancellationToken = default);
    }
}
