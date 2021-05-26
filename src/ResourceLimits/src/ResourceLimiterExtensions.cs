// Will be migrated to dotnet/runtime
// Pending dotnet API review

using System.Threading.Tasks;

namespace System.Threading.ResourceLimits
{
    public static class ResourceLimiterExtensions
    {
        public static ResourceLease Acquire(this ResourceLimiter limiter)
        {
            return limiter.Acquire(1);
        }

        public static ValueTask<ResourceLease> WaitAsync(this ResourceLimiter limiter, CancellationToken cancellationToken = default)
        {
            return limiter.WaitAsync(1, cancellationToken);
        }
        public static ResourceLease Acquire<TKey>(this AggregatedResourceLimiter<TKey> limiter, TKey resourceId)
        {
            return limiter.Acquire(resourceId, 1);
        }

        public static ValueTask<ResourceLease> WaitAsync<TKey>(this AggregatedResourceLimiter<TKey> limiter, TKey resourceId, CancellationToken cancellationToken = default)
        {
            return limiter.WaitAsync(resourceId, 1, cancellationToken);
        }
    }
}
