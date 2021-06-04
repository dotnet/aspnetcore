// Will be migrated to dotnet/runtime
// Pending dotnet API review

using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.RateLimits
{
    // Represents a limiter type that users interact with to determine if an operation can proceed
    public abstract class RateLimiter
    {
        // An estimated count of the underlying permits
        public abstract int AvailablePermits();

        // Fast synchronous attempt to acquire permits
        // Set permitCount to 0 to get whether permits are exhausted
        public abstract PermitLease Acquire(int permitCount);

        // Wait until the requested permits are available or permits can no longer be acquired
        // Set permitCount to 0 to wait until permits are replenished
        public abstract ValueTask<PermitLease> WaitAsync(int permitCount, CancellationToken cancellationToken = default);
    }
}
