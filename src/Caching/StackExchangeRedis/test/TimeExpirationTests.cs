// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Microsoft.Extensions.Caching.StackExchangeRedis;

public class TimeExpirationTests
{
    private const string SkipReason = "TODO: Disabled due to CI failure. " +
        "These tests require Redis server to be started on the machine. Make sure to change the value of" +
        "\"RedisTestConfig.RedisPort\" accordingly.";

    [Fact(Skip = SkipReason)]
    public void AbsoluteExpirationInThePastThrows()
    {
        var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
        var key = GetNameAndReset(cache);
        var value = new byte[1];

        var expected = DateTimeOffset.Now - TimeSpan.FromMinutes(1);
        ExceptionAssert.ThrowsArgumentOutOfRange(
            () =>
            {
                cache.Set(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(expected));
            },
            nameof(DistributedCacheEntryOptions.AbsoluteExpiration),
            "The absolute expiration value must be in the future.",
            expected);
    }

    [Fact(Skip = SkipReason)]
    public void AbsoluteExpirationExpires()
    {
        var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
        var key = GetNameAndReset(cache);
        var value = new byte[1];

        cache.Set(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(1)));

        byte[] result = cache.Get(key);
        Assert.Equal(value, result);

        for (int i = 0; i < 4 && (result != null); i++)
        {
            Thread.Sleep(TimeSpan.FromSeconds(0.5));
            result = cache.Get(key);
        }

        Assert.Null(result);
    }

    [Fact(Skip = SkipReason)]
    public void AbsoluteSubSecondExpirationExpiresImmediately()
    {
        var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
        var key = GetNameAndReset(cache);
        var value = new byte[1];

        cache.Set(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(0.25)));

        var result = cache.Get(key);
        Assert.Null(result);
    }

    [Fact(Skip = SkipReason)]
    public void NegativeRelativeExpirationThrows()
    {
        var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
        var key = GetNameAndReset(cache);
        var value = new byte[1];

        ExceptionAssert.ThrowsArgumentOutOfRange(() =>
        {
            cache.Set(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(-1)));
        },
        nameof(DistributedCacheEntryOptions.AbsoluteExpirationRelativeToNow),
        "The relative expiration value must be positive.",
        TimeSpan.FromMinutes(-1));
    }

    [Fact(Skip = SkipReason)]
    public void ZeroRelativeExpirationThrows()
    {
        var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
        var key = GetNameAndReset(cache);
        var value = new byte[1];

        ExceptionAssert.ThrowsArgumentOutOfRange(
            () =>
            {
                cache.Set(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.Zero));
            },
            nameof(DistributedCacheEntryOptions.AbsoluteExpirationRelativeToNow),
            "The relative expiration value must be positive.",
            TimeSpan.Zero);
    }

    [Fact(Skip = SkipReason)]
    public void RelativeExpirationExpires()
    {
        var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
        var key = GetNameAndReset(cache);
        var value = new byte[1];

        cache.Set(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(1)));

        var result = cache.Get(key);
        Assert.Equal(value, result);

        for (int i = 0; i < 4 && (result != null); i++)
        {
            Thread.Sleep(TimeSpan.FromSeconds(0.5));
            result = cache.Get(key);
        }
        Assert.Null(result);
    }

    [Fact(Skip = SkipReason)]
    public void RelativeSubSecondExpirationExpiresImmediately()
    {
        var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
        var key = GetNameAndReset(cache);
        var value = new byte[1];

        cache.Set(key, value, new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(0.25)));

        var result = cache.Get(key);
        Assert.Null(result);
    }

    [Fact(Skip = SkipReason)]
    public void NegativeSlidingExpirationThrows()
    {
        var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
        var key = GetNameAndReset(cache);
        var value = new byte[1];

        ExceptionAssert.ThrowsArgumentOutOfRange(() =>
        {
            cache.Set(key, value, new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(-1)));
        }, nameof(DistributedCacheEntryOptions.SlidingExpiration), "The sliding expiration value must be positive.", TimeSpan.FromMinutes(-1));
    }

    [Fact(Skip = SkipReason)]
    public void ZeroSlidingExpirationThrows()
    {
        var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
        var key = GetNameAndReset(cache);
        var value = new byte[1];

        ExceptionAssert.ThrowsArgumentOutOfRange(
            () =>
            {
                cache.Set(key, value, new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.Zero));
            },
            nameof(DistributedCacheEntryOptions.SlidingExpiration),
            "The sliding expiration value must be positive.",
            TimeSpan.Zero);
    }

    [Fact(Skip = SkipReason)]
    public void SlidingExpirationExpiresIfNotAccessed()
    {
        var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
        var key = GetNameAndReset(cache);
        var value = new byte[1];

        cache.Set(key, value, new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(1)));

        var result = cache.Get(key);
        Assert.Equal(value, result);

        Thread.Sleep(TimeSpan.FromSeconds(3.5));

        result = cache.Get(key);
        Assert.Null(result);
    }

    [Fact(Skip = SkipReason)]
    public void SlidingSubSecondExpirationExpiresImmediately()
    {
        var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
        var key = GetNameAndReset(cache);
        var value = new byte[1];

        cache.Set(key, value, new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(0.25)));

        var result = cache.Get(key);
        Assert.Null(result);
    }

    [Fact(Skip = SkipReason)]
    public void SlidingExpirationRenewedByAccess()
    {
        var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
        var key = GetNameAndReset(cache);
        var value = new byte[1];

        cache.Set(key, value, new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(1)));

        var result = cache.Get(key);
        Assert.Equal(value, result);

        for (int i = 0; i < 5; i++)
        {
            Thread.Sleep(TimeSpan.FromSeconds(0.5));

            result = cache.Get(key);
            Assert.Equal(value, result);
        }

        Thread.Sleep(TimeSpan.FromSeconds(3));
        result = cache.Get(key);
        Assert.Null(result);
    }

    [Fact(Skip = SkipReason)]
    public void SlidingExpirationRenewedByAccessUntilAbsoluteExpiration()
    {
        var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
        var key = GetNameAndReset(cache);
        var value = new byte[1];

        cache.Set(key, value, new DistributedCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromSeconds(1))
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(3)));

        var setTime = DateTime.Now;
        var result = cache.Get(key);
        Assert.Equal(value, result);

        for (int i = 0; i < 5; i++)
        {
            Thread.Sleep(TimeSpan.FromSeconds(0.5));

            result = cache.Get(key);
            Assert.NotNull(result);
            Assert.Equal(value, result);
        }

        while ((DateTime.Now - setTime).TotalSeconds < 4)
        {
            Thread.Sleep(TimeSpan.FromSeconds(0.5));
        }

        result = cache.Get(key);
        Assert.Null(result);
    }

    static string GetNameAndReset(IDistributedCache cache, [CallerMemberName] string caller = "")
    {
        cache.Remove(caller);
        return caller;
    }
}
