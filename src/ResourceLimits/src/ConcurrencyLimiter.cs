// Pending API review

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace System.Threading.ResourceLimits
{
    public class ConcurrencyLimiter : ResourceLimiter
    {
        private long _resourceCount;
        private readonly long _maxResourceCount;
        private object _lock = new object();
        private readonly ConcurrentQueue<ConcurrencyLimitRequest> _queue = new();

        // an inaccurate view of resources
        public override long EstimatedCount => Interlocked.Read(ref _resourceCount);

        public ConcurrencyLimiter(long resourceCount)
        {
            _resourceCount = resourceCount;
            _maxResourceCount = resourceCount;
        }

        // Fast synchronous attempt to acquire resources
        public override Resource Acquire(long requestedCount)
        {
            if (requestedCount < 0 || requestedCount > _maxResourceCount)
            {
                return Resource.FailNoopResource;
            }

            if (requestedCount == 0)
            {
                // TODO check if resources are exhausted
            }

            if (EstimatedCount >= requestedCount)
            {
                lock (_lock) // Check lock check
                {
                    if (EstimatedCount >= requestedCount)
                    {
                        Interlocked.Add(ref _resourceCount, -requestedCount);
                        return new Resource(
                            isAcquired: true,
                            state: null,
                            onDispose: resource => Release(requestedCount));
                    }
                }
            }

            return Resource.FailNoopResource;
        }

        // Wait until the requested resources are available
        public override ValueTask<Resource> AcquireAsync(long requestedCount, CancellationToken cancellationToken = default)
        {
            if (requestedCount < 0 || requestedCount > _maxResourceCount)
            {
                throw new InvalidOperationException("Limit exceeded");
            }

            if (EstimatedCount >= requestedCount)
            {
                lock (_lock) // Check lock check
                {
                    if (EstimatedCount >= requestedCount)
                    {
                        Interlocked.Add(ref _resourceCount, -requestedCount);
                        return ValueTask.FromResult(new Resource(
                            isAcquired: true,
                            state: null,
                            onDispose: resource => Release(requestedCount)));
                    }
                }
            }

            var registration = new ConcurrencyLimitRequest(requestedCount);
            _queue.Enqueue(registration);

            // handle cancellation
            return new ValueTask<Resource>(registration.TCS.Task);
        }

        private void Release(long releaseCount)
        {
            lock (_lock) // Check lock check
            {
                // Check for negative requestCount
                Interlocked.Add(ref _resourceCount, releaseCount);

                while (_queue.TryPeek(out var request))
                {
                    if (EstimatedCount >= request.Count)
                    {
                        // Request can be fulfilled
                        _queue.TryDequeue(out var requestToFulfill);

                        // requestToFulfill == request
                        requestToFulfill!.TCS.SetResult(new Resource(
                            isAcquired: true,
                            state: null,
                            onDispose: resource => Release(request.Count)));
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private class ConcurrencyLimitRequest
        {
            public ConcurrencyLimitRequest(long count)
            {
                Count = count;
                // Use VoidAsyncOperationWithData<T> instead
                TCS = new TaskCompletionSource<Resource>();
            }

            public long Count { get; }

            public TaskCompletionSource<Resource> TCS { get; }
        }
    }
}
