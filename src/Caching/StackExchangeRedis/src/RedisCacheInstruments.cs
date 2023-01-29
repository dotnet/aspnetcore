// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using System.Threading;

namespace Microsoft.Extensions.Caching.StackExchangeRedis;

internal static class RedisCacheInstruments
{
    private static readonly Meter _meter = new("Microsoft.Extensions.Caching.StackExchangeRedis", "1");

    private static readonly Counter<int> CacheHitsCounter = _meter.CreateCounter<int>("rediscache-hits");
    private static readonly Counter<int> CacheMissesCounter = _meter.CreateCounter<int>("rediscache-misses");
    private static readonly Counter<int> CacheSetsCounter = _meter.CreateCounter<int>("rediscache-sets");
    private static readonly Counter<int> CacheRefreshesCounter = _meter.CreateCounter<int>("rediscache-refreshes");

    public static void CacheHit()
    {
        CacheHitsCounter.Add(1);
    }

    public static void CacheMiss()
    {
        CacheMissesCounter.Add(1);
    }

    public static void CacheSet()
    {
        CacheSetsCounter.Add(1);
    }

    public static void CacheRefresh()
    {
        CacheRefreshesCounter.Add(1);
    }
}
