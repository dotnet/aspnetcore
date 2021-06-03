using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Runtime.RateLimits;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.RequestLimiter
{
    // TODO: update implementation with WaitAsync 
    public class AggregatedTokenBucketLimiter<TContext> : AggregatedRateLimiter<TContext> where TContext: IEquatable<TContext>
    {
        private readonly int _maxPermitCount;
        private readonly int _newPermitPerSecond;
        private readonly Timer _renewTimer;

        // TODO: This is racy
        private readonly ConcurrentDictionary<TContext, int> _cache = new();

        private static readonly RateLimitLease FailedLease = new(false);
        private static readonly RateLimitLease SuccessfulLease = new(true);

        public AggregatedTokenBucketLimiter(int maxPermitCount, int newPermitPerSecond)
        {
            _maxPermitCount = maxPermitCount;
            _newPermitPerSecond = newPermitPerSecond;

            // Start timer (5s for demo)
            _renewTimer = new Timer(Replenish, this, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        public override int AvailablePermits(TContext context)
        {
            return _cache.TryGetValue(context, out var count) ? count : 0;
        }

        public override PermitLease Acquire(TContext context, int permitCount)
        {
            if (permitCount > _maxPermitCount)
            {
                return FailedLease;
            }

            if (!_cache.TryGetValue(context, out var count))
            {
                if (_cache.TryAdd(context, _maxPermitCount - permitCount))
                {
                    return SuccessfulLease;
                }
            }

            while (true)
            {
                var newCount = count - permitCount;

                if (newCount < 0)
                {
                    return FailedLease;
                }

                if (_cache.TryUpdate(context, newCount, count))
                {
                    return SuccessfulLease;
                }

                if (!_cache.TryGetValue(context, out count))
                {
                    if (_cache.TryAdd(context, _maxPermitCount - permitCount))
                    {
                        return SuccessfulLease;
                    }
                }
            }
        }

        public override ValueTask<PermitLease> WaitAsync(TContext context, int permitCount, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        private static void Replenish(object? state)
        {
            // Return if Replenish already running to avoid concurrency.
            if (state is not AggregatedTokenBucketLimiter<TContext> limiter)
            {
                return;
            }

            var cache = limiter._cache;

            foreach (var entry in cache)
            {
                if (entry.Value >= limiter._maxPermitCount - limiter._newPermitPerSecond)
                {
                    if (cache.TryRemove(entry))
                    {
                        continue;
                    }
                }

                while (true)
                {
                    if (!cache.TryGetValue(entry.Key, out var newCount))
                    {
                        break;
                    }
                    if (cache.TryUpdate(entry.Key, Math.Min(limiter._maxPermitCount, newCount + limiter._newPermitPerSecond), newCount))
                    {
                        break;
                    }
                }
            }
        }

        private class RateLimitLease : PermitLease
        {
            public RateLimitLease(bool isAcquired)
            {
                IsAcquired = isAcquired;
            }

            public override bool IsAcquired { get; }

            public override void Dispose() { }

            public override bool TryGetMetadata(MetadataName metadataName, [NotNullWhen(true)] out object? metadata)
            {
                metadata = null;
                return false;
            }
        }
    }
}
