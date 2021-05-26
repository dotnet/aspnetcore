// Will be migrated to dotnet/runtime
// Pending dotnet API review

using System.Threading.Tasks;

namespace System.Threading.ResourceLimits
{
    // Represents a limiter type that users interact with to determine if an operation can proceed
    public abstract class ResourceLimiter
    {
        // An estimated count of the underlying resources
        public abstract long EstimatedCount { get; }

        // Fast synchronous attempt to acquire resources
        // Set requestedCount to 0 to get whether resource limit has been reached
        public abstract ResourceLease Acquire(long requestedCount);

        // Wait until the requested resources are available or resources can no longer be acquired
        // Set requestedCount to 0 to wait until resource is replenished
        public abstract ValueTask<ResourceLease> WaitAsync(long requestedCount, CancellationToken cancellationToken = default);
    }
}
