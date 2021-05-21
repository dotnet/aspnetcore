// Pending API review

using System.Threading.Tasks;

namespace System.Threading.ResourceLimits
{
    public static class ResourceLimiterExtensions
    {
        public static Resource Acquire(this ResourceLimiter limiter)
        {
            return limiter.Acquire(1);
        }

        public static ValueTask<Resource> AcquireAsync(this ResourceLimiter limiter, CancellationToken cancellationToken = default)
        {
            return limiter.AcquireAsync(1, cancellationToken);
        }
        public static Resource Acquire<TKey>(this AggregatedResourceLimiter<TKey> limiter, TKey resourceId)
        {
            return limiter.Acquire(resourceId, 1);
        }

        public static ValueTask<Resource> AcquireAsync<TKey>(this AggregatedResourceLimiter<TKey> limiter, TKey resourceId, CancellationToken cancellationToken = default)
        {
            return limiter.AcquireAsync(resourceId, 1, cancellationToken);
        }
    }
}
