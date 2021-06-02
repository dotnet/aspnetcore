// Will be migrated to dotnet/runtime
// Pending dotnet API review

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.RateLimits
{
    public class TokenBucketRateLimiter : RateLimiter
    {
        private int _permitCount;
        private int _queueCount;

        private readonly Timer _renewTimer;
        private readonly object _lock = new();
        private readonly TokenBucketRateLimiterOptions _options;
        private readonly Deque<RequestRegistration> _queue = new();

        private static readonly PermitLease SuccessfulLease = new(true, null, null);

        public override int AvailablePermits => _permitCount;

        public TokenBucketRateLimiter(TokenBucketRateLimiterOptions options)
        {
            _permitCount = options.PermitLimit;
            _options = options;

            // Assume self replenishing, add option for external replenishment
            _renewTimer = new Timer(Replenish, this, _options.ReplenishmentPeriod, _options.ReplenishmentPeriod);
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
                if (AvailablePermits > 0)
                {
                    return SuccessfulLease;
                }

                return CreateFailedPermitLease();
            }

            // These amounts of resources can never be acquired
            if (Interlocked.Add(ref _permitCount, -permitCount) >= 0)
            {
                return SuccessfulLease;
            }

            Interlocked.Add(ref _permitCount, permitCount);

            return CreateFailedPermitLease();
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

            if (Interlocked.Add(ref _permitCount, -permitCount) >= 0)
            {
                // Perf: static failed/successful value tasks?
                return ValueTask.FromResult(SuccessfulLease);
            }

            Interlocked.Add(ref _permitCount, permitCount);

            // Don't queue if queue limit reached
            if (_queueCount + permitCount > _options.QueueLimit)
            {
                return ValueTask.FromResult(CreateFailedPermitLease());
            }

            var registration = new RequestRegistration(permitCount);
            _queue.EnqueueTail(registration);
            Interlocked.Add(ref _permitCount, permitCount);

            // handle cancellation
            return new ValueTask<PermitLease>(registration.TCS.Task);
        }

        private PermitLease CreateFailedPermitLease()
        {
            var replenishAmount = _permitCount - AvailablePermits + _queueCount;
            var replenishPeriods = (replenishAmount / _options.TokensPerPeriod) + 1;

            return new PermitLease(
                false,
                new Dictionary<MetadataName, object?>
                {
                    {
                        MetadataName.RetryAfter,
                        TimeSpan.FromTicks(_options.ReplenishmentPeriod.Ticks*replenishPeriods)
                    }
                },
                null);
        }

        public static void Replenish(object? state)
        {
            // Return if Replenish already running to avoid concurrency.
            if (state is not TokenBucketRateLimiter limiter)
            {
                return;
            }

            var availablePermits = limiter.AvailablePermits;
            var options = limiter._options;
            var maxPermits = options.PermitLimit;

            if (availablePermits < maxPermits)
            {
                var resoucesToAdd = Math.Min(options.TokensPerPeriod, maxPermits - availablePermits);
                Interlocked.Add(ref limiter._permitCount, resoucesToAdd);
            }

            // Process queued requests
            var queue = limiter._queue;
            lock (limiter._lock)
            {
                while (queue.Count > 0)
                {
                    var nextPendingRequest =
                          options.PermitsExhaustedMode == PermitsExhaustedMode.EnqueueIncomingRequest
                          ? queue.PeekHead()
                          : queue.PeekTail();

                    if (Interlocked.Add(ref limiter._permitCount, -nextPendingRequest.Count) >= 0)
                    {
                        // Request can be fulfilled
                        var request =
                            options.PermitsExhaustedMode == PermitsExhaustedMode.EnqueueIncomingRequest
                            ? queue.DequeueHead()
                            : queue.DequeueTail();
                        Interlocked.Add(ref limiter._queueCount, -request.Count);

                        // requestToFulfill == request
                        request.TCS.SetResult(SuccessfulLease);
                    }
                    else
                    {
                        // Request cannot be fulfilled
                        Interlocked.Add(ref limiter._permitCount, nextPendingRequest.Count);
                        break;
                    }
                }
            }
        }

        private struct RequestRegistration
        {
            public RequestRegistration(int permitCount)
            {
                Count = permitCount;
                // Use VoidAsyncOperationWithData<T> instead
                TCS = new TaskCompletionSource<PermitLease>();
            }

            public int Count { get; }

            public TaskCompletionSource<PermitLease> TCS { get; }
        }
    }
}
