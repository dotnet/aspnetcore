// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using StackExchange.Redis;

namespace Microsoft.Extensions.Caching.StackExchangeRedis;

internal readonly struct RedisEntry
{
    public RedisValue? Data { get; }

    public TimeSpan? SlidingWindow { get; }

    public DateTimeOffset? AbsoluteExpiration { get; }

    internal RedisEntry(RedisValue? data, DateTimeOffset? absoluteExpiration, TimeSpan? slidingWindow)
    {
        Data = data;
        SlidingWindow = slidingWindow;
        AbsoluteExpiration = absoluteExpiration;
    }

    public static RedisEntry Get(RedisValue[] results)
    {
        const int DataValue = 0;
        const int SlidingWindowValue = 1;
        var (absolute, window) = ExtractExpiration(results, SlidingWindowValue);

        RedisValue? data = null;

        if (results.Length > 0)
        {
            data = results[DataValue];
        }

        return new(data, absolute, window);
    }

    public static RedisEntry Refresh(RedisValue[] results)
    {
        const int SlidingWindowValue = 0;
        var (absolute, window) = ExtractExpiration(results, SlidingWindowValue);

        return new(null, absolute, window);
    }

    public void Deconstruct(out RedisValue? data, out DateTimeOffset? absoluteExpiration, out TimeSpan? slidingWindow)
    {
        data = Data;
        absoluteExpiration = AbsoluteExpiration;
        slidingWindow = SlidingWindow;
    }

    private static (DateTimeOffset? absoluteExpiration, TimeSpan? slidingWindow) ExtractExpiration(RedisValue[] values, int startIndex)
    {
        DateTimeOffset? absoluteExpiration = null;
        TimeSpan? slidingWindow = null;

        if (values.Length > startIndex)
        {
            var window = values[startIndex];

            if (!window.IsNullOrEmpty)
            {
                slidingWindow = new TimeSpan((long)window);
            }

            if (values.Length > startIndex + 1)
            {
                var absolute = values[startIndex + 1];

                if (!absolute.IsNullOrEmpty)
                {
                    absoluteExpiration = new DateTimeOffset((long)absolute, TimeSpan.Zero);
                }
            }
        }

        return (absoluteExpiration, slidingWindow);
    }
}
