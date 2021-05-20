// Pending API review

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace System.Threading.ResourceLimits
{
    public static class ResourceLimiterExtensions
    {
        public static bool TryAcquire(this ResourceLimiter limiter, [NotNullWhen(true)] out Resource? resource)
        {
            return limiter.TryAcquire(1, out resource);
        }

        public static ValueTask<Resource> AcquireAsync(this ResourceLimiter limiter, CancellationToken cancellationToken = default)
        {
            return limiter.AcquireAsync(1, cancellationToken);
        }
        public static bool TryAcquire<TKey>(this AggregatedResourceLimiter<TKey> limiter, TKey resourceId, [NotNullWhen(true)] out Resource? resource)
        {
            return limiter.TryAcquire(resourceId, 1, out resource);
        }

        public static ValueTask<Resource> AcquireAsync<TKey>(this AggregatedResourceLimiter<TKey> limiter, TKey resourceId, CancellationToken cancellationToken = default)
        {
            return limiter.AcquireAsync(resourceId, 1, cancellationToken);
        }
    }
}
