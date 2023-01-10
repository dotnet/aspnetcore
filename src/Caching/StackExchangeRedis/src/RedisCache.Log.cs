// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Caching.StackExchangeRedis;

public partial class RedisCache
{
    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Trace, "Redis cache hit for key {CacheKey}.", EventName = "RedisCacheHit")]
        public static partial void RedisCacheHit(ILogger logger, string cacheKey);

        [LoggerMessage(2, LogLevel.Trace, "Redis cache miss for key {CacheKey}.", EventName = "RedisCacheMiss")]
        public static partial void RedisCacheMiss(ILogger logger, string cacheKey);

        [LoggerMessage(3, LogLevel.Trace, "Setting Redis cache key {CacheKey}.", EventName = "RedisCacheSetKey")]
        public static partial void RedisCacheSet(ILogger logger, string cacheKey);

        [LoggerMessage(4, LogLevel.Trace, "Refreshing Redis cache key {CacheKey}.", EventName = "RedisCacheRefreshKey")]
        public static partial void RedisCacheRefresh(ILogger logger, string cacheKey);
    }
}
