// Pending API review

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace System.Threading.ResourceLimits
{
    public abstract class ResourceLimiter
    {
        // an inaccurate view of resources
        public abstract long EstimatedCount { get; }

        // Fast synchronous attempt to acquire resources
        public abstract bool TryAcquire(long requestedCount, [NotNullWhen(true)] out Resource? resource);

        // Wait until the requested resources are available
        // If unsuccessful, throw
        public abstract ValueTask<Resource> AcquireAsync(long requestedCount, CancellationToken cancellationToken = default);
    }
}
