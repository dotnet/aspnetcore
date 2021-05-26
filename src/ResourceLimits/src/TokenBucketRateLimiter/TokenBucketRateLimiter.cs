// Will be migrated to dotnet/runtime
// Pending dotnet API review

using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Threading.ResourceLimits
{
    // TODO: rework implementation with options and queuing
    public class TokenBucketRateLimiter : ResourceLimiter
    {
        private long _resourceCount;
        private readonly long _maxResourceCount;
        private readonly long _newResourcePerSecond;
        private Timer _renewTimer;
        private readonly Queue<RateLimitRequest> _queue = new Queue<RateLimitRequest>();
        private object _lock = new object();

        public override long EstimatedCount => Interlocked.Read(ref _resourceCount);

        public TokenBucketRateLimiter(long resourceCount, long newResourcePerSecond)
        {
            _resourceCount = 0;
            _maxResourceCount = resourceCount; // Another variable for max resource count?
            _newResourcePerSecond = newResourcePerSecond;

            // Start timer (5s for demo)
            _renewTimer = new Timer(Replenish, this, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        public override ResourceLease Acquire(long requestedCount)
        {
            if (Interlocked.Add(ref _resourceCount, requestedCount) <= _maxResourceCount)
            {
                return ResourceLease.SuccessfulAcquisition;
            }

            Interlocked.Add(ref _resourceCount, -requestedCount);
            return ResourceLease.FailedAcquisition;
        }

        public override ValueTask<ResourceLease> WaitAsync(long requestedCount, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Add(ref _resourceCount, requestedCount) <= _maxResourceCount)
            {
                return ValueTask.FromResult(ResourceLease.SuccessfulAcquisition);
            }

            Interlocked.Add(ref _resourceCount, -requestedCount);

            var registration = new RateLimitRequest(requestedCount);
            _queue.Enqueue(registration);

            // handle cancellation
            return new ValueTask<ResourceLease>(registration.TCS.Task);
        }

        private static void Replenish(object? state)
        {
            // Return if Replenish already running to avoid concurrency.
            var limiter = state as TokenBucketRateLimiter;

            if (limiter == null)
            {
                return;
            }

            if (limiter._resourceCount > 0)
            {
                var resoucesToDeduct = Math.Min(limiter._newResourcePerSecond, limiter._resourceCount);
                Interlocked.Add(ref limiter._resourceCount, -resoucesToDeduct);
            }

            // Process queued requests
            var queue = limiter._queue;
            lock (limiter._lock)
            {
                while (queue.TryPeek(out var request))
                {
                    if (Interlocked.Add(ref limiter._resourceCount, request.Count) <= limiter._maxResourceCount)
                    {
                        // Request can be fulfilled
                        queue.TryDequeue(out var requestToFulfill);
                        requestToFulfill.TCS.SetResult(ResourceLease.SuccessfulAcquisition);
                    }
                    else
                    {
                        // Request cannot be fulfilled
                        Interlocked.Add(ref limiter._resourceCount, -request.Count);
                        break;
                    }
                }
            }
        }

        private struct RateLimitRequest
        {
            public RateLimitRequest(long count)
            {
                Count = count;
                // Use VoidAsyncOperationWithData<T> instead
                TCS = new TaskCompletionSource<ResourceLease>();
            }

            public long Count { get; }

            public TaskCompletionSource<ResourceLease> TCS { get; }
        }
    }
}
