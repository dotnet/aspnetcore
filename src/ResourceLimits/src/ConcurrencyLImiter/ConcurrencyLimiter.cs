// Will be migrated to dotnet/runtime
// Pending dotnet API review

using System.Collections.Generic;
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

        private static readonly PermitLease SuccessfulLease = new(false, null, null);
        private static readonly PermitLease FailedLease = new(false, null, null);

        public override int AvailablePermits => _permitCount;

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
                return AvailablePermits > 0 ? SuccessfulLease : FailedLease;
            }

            // Perf: Check SemaphoreSlim implementation instead of locking
            if (AvailablePermits >= permitCount)
            {
                lock (_lock)
                {
                    if (AvailablePermits >= permitCount)
                    {
                        _permitCount -= permitCount;
                        return new PermitLease(
                            isAcquired: true,
                            metadata: null,
                            // Perf: This captures state
                            disposable: new ConcurrencyLease(this, permitCount));
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
            if (permitCount == 0 && AvailablePermits > 0)
            {
                // Perf: static failed/successful value tasks?
                return ValueTask.FromResult(SuccessfulLease);
            }

            // Perf: Check SemaphoreSlim implementation instead of locking
            lock (_lock) // Check lock check
            {
                if (AvailablePermits >= permitCount)
                {
                    _permitCount -= permitCount;
                    return ValueTask.FromResult(new PermitLease(
                        isAcquired: true,
                        metadata: null,
                        // Perf: This captures state
                        disposable: new ConcurrencyLease(this, permitCount)));
                }

                // Don't queue if queue limit reached
                if (_queueCount + permitCount > _options.QueueLimit)
                {
                    // Perf: static failed/successful value tasks?
                    return ValueTask.FromResult(FailedLease);
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

                    if (AvailablePermits >= nextPendingRequest.Count)
                    {
                        var request =
                            _options.PermitsExhaustedMode == PermitsExhaustedMode.EnqueueIncomingRequest
                            ? _queue.DequeueHead()
                            : _queue.DequeueTail();

                        _permitCount -= request.Count;
                        _queueCount -= request.Count;

                        // requestToFulfill == request
                        request.TCS.SetResult(new PermitLease(
                            isAcquired: true,
                            metadata: null,
                            disposable: new ConcurrencyLease(this, request.Count)));
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        // Perf: this allocation can be saved via an ID and associated count stored in a dictionary
        // tracking outstanding permit leases on the limiter itself. Currently implemented as a class
        // for simplicity
        private class ConcurrencyLease : IDisposable
        {
            private int _released = 0;
            private readonly ConcurrencyLimiter _limiter;
            private readonly int _count;

            public ConcurrencyLease(ConcurrencyLimiter limiter, int count)
            {
                _limiter = limiter;
                _count = count;
            }

            public void Dispose()
            {
                if (Interlocked.CompareExchange(ref _released, 1, 0) == 0)
                {
                    _limiter.Release(_count);
                }
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
