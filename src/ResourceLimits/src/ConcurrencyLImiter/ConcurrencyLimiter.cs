// Will be migrated to dotnet/runtime
// Pending dotnet API review

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.RateLimits
{
    public class ConcurrencyLimiter : RateLimiter
    {
        private int _permitCount;
        private int _queueCount;

        private readonly object _lock = new();
        private readonly ConcurrencyLimiterOptions _options;
        private readonly Deque<RequestRegistration> _queue = new();

        private static readonly ConcurrencyLease SuccessfulLease = new(true, null, 0);
        private static readonly ConcurrencyLease FailedLease = new(false, null, 0);

        public override int AvailablePermits() => _permitCount;

        public ConcurrencyLimiter(ConcurrencyLimiterOptions options)
        {
            _options = options;
            _permitCount = _options.PermitLimit;
        }

        // Fast synchronous attempt to acquire resources
        public override PermitLease Acquire(int permitCount)
        {
            // These amounts of resources can never be acquired
            if (permitCount < 0 || permitCount > _options.PermitLimit)
            {
                throw new ArgumentOutOfRangeException();
            }

            // Return SuccessfulAcquisition or FailedAcquisition depending to indicate limiter state
            if (permitCount == 0)
            {
                return AvailablePermits() > 0 ? SuccessfulLease : FailedLease;
            }

            // Perf: Check SemaphoreSlim implementation instead of locking
            if (AvailablePermits() >= permitCount)
            {
                lock (_lock)
                {
                    if (AvailablePermits() >= permitCount)
                    {
                        _permitCount -= permitCount;
                        return new ConcurrencyLease(true, this, permitCount);
                    }
                }
            }

            return FailedLease;
        }

        // Wait until the requested resources are available
        public override ValueTask<PermitLease> WaitAsync(int permitCount, CancellationToken cancellationToken = default)
        {
            // These amounts of resources can never be acquired
            if (permitCount < 0 || permitCount > _options.PermitLimit)
            {
                throw new ArgumentOutOfRangeException();
            }

            // Return SuccessfulAcquisition if requestedCount is 0 and resources are available
            if (permitCount == 0 && AvailablePermits() > 0)
            {
                // Perf: static failed/successful value tasks?
                return ValueTask.FromResult((PermitLease)SuccessfulLease);
            }

            // Perf: Check SemaphoreSlim implementation instead of locking
            lock (_lock) // Check lock check
            {
                if (AvailablePermits() >= permitCount)
                {
                    _permitCount -= permitCount;
                    return ValueTask.FromResult((PermitLease)new ConcurrencyLease(true, this, permitCount));
                }

                // Don't queue if queue limit reached
                if (_queueCount + permitCount > _options.QueueLimit)
                {
                    // Perf: static failed/successful value tasks?
                    return ValueTask.FromResult((PermitLease)FailedLease);
                }

                var request = new RequestRegistration(permitCount);
                _queue.EnqueueTail(request);
                _queueCount += permitCount;

                // TODO: handle cancellation
                return new ValueTask<PermitLease>(request.TCS.Task);
            }
        }

        private void Release(int releaseCount)
        {
            lock (_lock) // Check lock check
            {
                _permitCount += releaseCount;

                while (_queue.Count > 0)
                {
                    var nextPendingRequest =
                        _options.PermitsExhaustedMode == PermitsExhaustedMode.EnqueueIncomingRequest
                        ? _queue.PeekHead()
                        : _queue.PeekTail(); 

                    if (AvailablePermits() >= nextPendingRequest.Count)
                    {
                        var request =
                            _options.PermitsExhaustedMode == PermitsExhaustedMode.EnqueueIncomingRequest
                            ? _queue.DequeueHead()
                            : _queue.DequeueTail();

                        _permitCount -= request.Count;
                        _queueCount -= request.Count;

                        // requestToFulfill == request
                        request.TCS.SetResult(new ConcurrencyLease(true, this, request.Count));
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private class ConcurrencyLease : PermitLease
        {
            private bool _disposed;
            private readonly ConcurrencyLimiter? _limiter;
            private readonly int _count;

            public ConcurrencyLease(bool isAcquired, ConcurrencyLimiter? limiter, int count)
            {
                IsAcquired = isAcquired;
                _limiter = limiter;
                _count = count;
            }

            public override bool IsAcquired { get; }

            public override bool TryGetMetadata(MetadataName metadataName, [NotNullWhen(true)] out object? metadata)
            {
                metadata = default;
                return false;
            }

            public override void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;

                _limiter?.Release(_count);
            }
        }

        private struct RequestRegistration
        {
            public RequestRegistration(int requestedCount)
            {
                Count = requestedCount;
                // Perf: Use AsyncOperation<TResult> instead
                TCS = new TaskCompletionSource<PermitLease>();
            }

            public int Count { get; }

            public TaskCompletionSource<PermitLease> TCS { get; }
        }
    }
}
