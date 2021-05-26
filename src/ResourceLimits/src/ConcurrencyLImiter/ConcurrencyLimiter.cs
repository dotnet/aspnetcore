// Will be migrated to dotnet/runtime
// Pending dotnet API review

using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Threading.ResourceLimits
{
    public class ConcurrencyLimiter : ResourceLimiter
    {
        private readonly ConcurrencyLimiterOptions _options;
        private readonly Deque<ConcurrencyLimitRequest> _queue = new();

        private long _resourceCount;
        private long _queueCount;
        private object _lock = new object();

        // This implementation counts down resources, like 
        public override long EstimatedCount => Interlocked.Read(ref _resourceCount);

        public ConcurrencyLimiter(ConcurrencyLimiterOptions options)
        {
            _options = options;
            _resourceCount = _options.ResourceLimit;
        }

        // Fast synchronous attempt to acquire resources
        public override ResourceLease Acquire(long requestedCount)
        {
            // These amounts of resources can never be acquired
            if (requestedCount < 0 || requestedCount > _options.ResourceLimit)
            {
                return ResourceLease.FailedAcquisition;
            }

            // Return SuccessfulAcquisition or FailedAcquisition depending to indicate limiter state
            if (requestedCount == 0)
            {
                return EstimatedCount > 0 ? ResourceLease.SuccessfulAcquisition : ResourceLease.FailedAcquisition;
            }

            // Perf: Check SemaphoreSlim implementation instead of locking
            if (EstimatedCount >= requestedCount)
            {
                lock (_lock)
                {
                    if (EstimatedCount >= requestedCount)
                    {
                        Interlocked.Add(ref _resourceCount, -requestedCount);
                        return new ResourceLease(
                            isAcquired: true,
                            state: null,
                            // Perf: This captures state
                            onDispose: resource => Release(requestedCount));
                    }
                }
            }

            return ResourceLease.FailedAcquisition;
        }

        // Wait until the requested resources are available
        public override ValueTask<ResourceLease> WaitAsync(long requestedCount, CancellationToken cancellationToken = default)
        {
            // These amounts of resources can never be acquired
            if (requestedCount < 0 || requestedCount > _options.ResourceLimit)
            {
                return ValueTask.FromResult(ResourceLease.FailedAcquisition);
            }

            // Return SuccessfulAcquisition if requestedCount is 0 and resources are available
            if (requestedCount == 0 && EstimatedCount > 0)
            {
                return ValueTask.FromResult(ResourceLease.SuccessfulAcquisition);
            }

            // Perf: Check SemaphoreSlim implementation instead of locking
            lock (_lock) // Check lock check
            {
                if (EstimatedCount >= requestedCount)
                {
                    Interlocked.Add(ref _resourceCount, -requestedCount);
                    return ValueTask.FromResult(new ResourceLease(
                        isAcquired: true,
                        state: null,
                        // Perf: This captures state
                        onDispose: resource => Release(requestedCount)));
                }

                // Don't queue if queue limit reached
                if (Interlocked.Read(ref _queueCount) + requestedCount > _options.MaxQueueLimit)
                {
                    return ValueTask.FromResult(ResourceLease.FailedAcquisition);
                }

                var request = new ConcurrencyLimitRequest(requestedCount);
                _queue.EnqueueTail(request);
                Interlocked.Add(ref _queueCount, requestedCount);

                // TODO: handle cancellation
                return new ValueTask<ResourceLease>(request.TCS.Task);
            }
        }

        private void Release(long releaseCount)
        {
            lock (_lock) // Check lock check
            {
                Interlocked.Add(ref _resourceCount, releaseCount);

                while (_queue.Count > 0)
                {
                    var nextPendingRequest =
                        _options.ResourceDepletedMode == ResourceDepletedMode.EnqueueIncomingRequest
                        ? _queue.PeekHead()
                        : _queue.PeekTail(); 

                    if (EstimatedCount >= nextPendingRequest.Count)
                    {
                        var request =
                            _options.ResourceDepletedMode == ResourceDepletedMode.EnqueueIncomingRequest
                            ? _queue.DequeueHead()
                            : _queue.DequeueTail();

                        Interlocked.Add(ref _resourceCount, -request.Count);
                        Interlocked.Add(ref _queueCount, -request.Count);

                        // requestToFulfill == request
                        request.TCS.SetResult(new ResourceLease(
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

        private struct ConcurrencyLimitRequest
        {
            public ConcurrencyLimitRequest(long requestedCount)
            {
                Count = requestedCount;
                // Perf: Use AsyncOperation<TResult> instead
                TCS = new TaskCompletionSource<ResourceLease>();
            }

            public long Count { get; }

            public TaskCompletionSource<ResourceLease> TCS { get; }
        }
    }
}
