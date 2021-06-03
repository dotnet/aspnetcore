using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Runtime.RateLimits;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.RequestLimiter
{
    // TODO: update implementation with WaitAsync 
    public class IPAggregatedRateLimiter : AggregatedRateLimiter<HttpContext>
    {
        private int _permitCount;

        private readonly int _maxPermitCount;
        private readonly int _newPermitPerSecond;
        private readonly Timer _renewTimer;
        // TODO: This is racy
        private readonly ConcurrentDictionary<IPAddress, int> _cache = new();

        private static readonly RateLimitLease FailedLease = new RateLimitLease(false);
        private static readonly RateLimitLease SuccessfulLease = new RateLimitLease(true);

        public IPAggregatedRateLimiter(int permitCount, int newPermitPerSecond)
        {
            _permitCount = permitCount;
            _maxPermitCount = permitCount;
            _newPermitPerSecond = newPermitPerSecond;

            // Start timer (5s for demo)
            _renewTimer = new Timer(Replenish, this, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        public override int AvailablePermits(HttpContext context)
        {
            if (context.Connection.RemoteIpAddress == null)
            {
                // Unknown IP?
                return 0;
            }

            return _cache.TryGetValue(context.Connection.RemoteIpAddress, out var count) ? count : 0;
        }

        public override PermitLease Acquire(HttpContext context, int permitCount)
        {
            if (permitCount > _maxPermitCount)
            {
                return FailedLease;
            }

            if (context.Connection.RemoteIpAddress == null)
            {
                // TODO: how should this case be handled?
                return SuccessfulLease;
            }

            var key = context.Connection.RemoteIpAddress;

            if (!_cache.TryGetValue(key, out var count))
            {
                if (_cache.TryAdd(key, _maxPermitCount - permitCount))
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

                if (_cache.TryUpdate(key, newCount, count))
                {
                    return SuccessfulLease;
                }

                if (!_cache.TryGetValue(key, out count))
                {
                    if (_cache.TryAdd(key, _maxPermitCount - permitCount))
                    {
                        return SuccessfulLease;
                    }
                }
            }
        }

        public override ValueTask<PermitLease> WaitAsync(HttpContext context, int permitCount, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        private static void Replenish(object? state)
        {
            // Return if Replenish already running to avoid concurrency.
            if (state is not IPAggregatedRateLimiter limiter)
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
