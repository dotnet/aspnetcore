// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.Extensions.Caching.StackExchangeRedis;

[EventSource(Name = "Microsoft-Extensions-Caching-StackExchangeRedis")]
internal class RedisCacheEventSource : EventSource
{
    public static readonly RedisCacheEventSource Log = new();

    private PollingCounter? _totalCacheHitsCounter;
    private PollingCounter? _totalCacheMissesCounter;
    private PollingCounter? _totalCacheSetsCounter;
    private PollingCounter? _totalCacheRefreshesCounter;

    private long _totalCacheHits;
    private long _totalCacheMisses;
    private long _totalCacheSets;
    private long _totalCacheRefreshes;


    private RedisCacheEventSource()
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Event(1, Level = EventLevel.Informational)]
    public void CacheHit()
    {
        Interlocked.Increment(ref _totalCacheHits);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Event(2, Level = EventLevel.Informational)]
    public void CacheMiss()
    {
        Interlocked.Increment(ref _totalCacheMisses);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Event(3, Level = EventLevel.Informational)]
    public void CacheSet()
    {
        Interlocked.Increment(ref _totalCacheSets);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Event(4, Level = EventLevel.Informational)]
    public void CacheRefresh()
    {
        Interlocked.Increment(ref _totalCacheRefreshes);
    }

    protected override void OnEventCommand(EventCommandEventArgs command)
    {
        if (command.Command == EventCommand.Enable)
        {
            // This is the convention for initializing counters in the RuntimeEventSource (lazily on the first enable command).
            // They aren't disabled afterwards...

            _totalCacheHitsCounter ??= new PollingCounter("total-rediscache-hits", this, () => Volatile.Read(ref _totalCacheHits))
            {
                DisplayName = "Total Redis Cache Hits",
            };

            _totalCacheMissesCounter ??= new PollingCounter("total-rediscache-misses", this, () => Volatile.Read(ref _totalCacheMisses))
            {
                DisplayName = "Total Redis Cache Misses",
            };

            _totalCacheSetsCounter ??= new PollingCounter("total-rediscache-sets", this, () => Volatile.Read(ref _totalCacheSets))
            {
                DisplayName = "Total Redis Cache Sets",
            };

            _totalCacheRefreshesCounter ??= new PollingCounter("total-rediscache-refreshes", this, () => Volatile.Read(ref _totalCacheRefreshes))
            {
                DisplayName = "Total Redis Cache Refreshes",
            };
        }
    }
}
