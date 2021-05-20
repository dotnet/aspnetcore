using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading;
using System.Threading.ResourceLimits;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RequestLimiter
{
    public class IPAggregatedRateLimiter : AggregatedResourceLimiter<HttpContext>
    {
        private long _resourceCount;
        private readonly long _maxResourceCount;
        private readonly long _newResourcePerSecond;

        private Timer _renewTimer;
        // This is racy
        private ConcurrentDictionary<IPAddress, long> _cache = new ConcurrentDictionary<IPAddress, long>();

        public IPAggregatedRateLimiter(long resourceCount, long newResourcePerSecond)
        {
            _resourceCount = resourceCount;
            _maxResourceCount = resourceCount;
            _newResourcePerSecond = newResourcePerSecond;

            // Start timer (5s for demo)
            _renewTimer = new Timer(Replenish, this, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        public override long EstimatedCount(HttpContext resourceId)
        {
            if (resourceId.Connection.RemoteIpAddress == null)
            {
                // Unknown IP?
                return 0;
            }

            return _cache.TryGetValue(resourceId.Connection.RemoteIpAddress, out var count) ? count : 0;
        }

        public override bool TryAcquire(HttpContext resourceId, long requestedCount, [NotNullWhen(true)] out Resource? resource)
        {
            resource = Resource.NoopResource;
            if (requestedCount > _maxResourceCount)
            {
                return false;
            }

            if (resourceId.Connection.RemoteIpAddress == null)
            {
                return true;
            }

            var key = resourceId.Connection.RemoteIpAddress;

            if (!_cache.TryGetValue(key, out var count))
            {
                if (_cache.TryAdd(key, requestedCount))
                {
                    return true;
                }
            }

            while (true)
            {
                var newCount = count + requestedCount;
                if (_cache.TryUpdate(key, count + requestedCount, count))
                {
                    if (newCount > _maxResourceCount)
                    {
                        return false;
                    }

                    return true;
                }
                if (!_cache.TryGetValue(key, out count))
                {
                    if (_cache.TryAdd(key, requestedCount))
                    {
                        return true;
                    }
                }
            }
        }

        public override ValueTask<Resource> AcquireAsync(HttpContext resourceId, long requestedCount, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        private static void Replenish(object? state)
        {
            // Return if Replenish already running to avoid concurrency.
            var limiter = state as IPAggregatedRateLimiter;

            if (limiter == null)
            {
                return;
            }

            var cache = limiter._cache;

            foreach (var entry in cache)
            {
                if (entry.Value < limiter._newResourcePerSecond)
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
                    if (cache.TryUpdate(entry.Key, Math.Max(0, newCount - limiter._newResourcePerSecond), newCount))
                    {
                        break;
                    }
                }
            }
        }
    }
}
