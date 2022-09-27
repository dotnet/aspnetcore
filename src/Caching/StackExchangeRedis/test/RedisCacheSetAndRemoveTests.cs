// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Caching.Distributed;
using Xunit;

namespace Microsoft.Extensions.Caching.StackExchangeRedis;

public class RedisCacheSetAndRemoveTests
{
    private const string SkipReason = "TODO: Disabled due to CI failure. " +
        "These tests require Redis server to be started on the machine. Make sure to change the value of" +
        "\"RedisTestConfig.RedisPort\" accordingly.";

    [Fact(Skip = SkipReason)]
    public void GetMissingKeyReturnsNull()
    {
        var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
        string key = "non-existent-key";

        var result = cache.Get(key);
        Assert.Null(result);
    }

    [Fact(Skip = SkipReason)]
    public void SetAndGetReturnsObject()
    {
        var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
        var value = new byte[1];
        string key = "myKey";

        cache.Set(key, value);

        var result = cache.Get(key);
        Assert.Equal(value, result);
    }

    [Fact(Skip = SkipReason)]
    public void SetAndGetWorksWithCaseSensitiveKeys()
    {
        var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
        var value = new byte[1];
        string key1 = "myKey";
        string key2 = "Mykey";

        cache.Set(key1, value);

        var result = cache.Get(key1);
        Assert.Equal(value, result);

        result = cache.Get(key2);
        Assert.Null(result);
    }

    [Fact(Skip = SkipReason)]
    public void SetAlwaysOverwrites()
    {
        var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
        var value1 = new byte[1] { 1 };
        string key = "myKey";

        cache.Set(key, value1);
        var result = cache.Get(key);
        Assert.Equal(value1, result);

        var value2 = new byte[1] { 2 };
        cache.Set(key, value2);
        result = cache.Get(key);
        Assert.Equal(value2, result);
    }

    [Fact(Skip = SkipReason)]
    public void RemoveRemoves()
    {
        var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
        var value = new byte[1];
        string key = "myKey";

        cache.Set(key, value);
        var result = cache.Get(key);
        Assert.Equal(value, result);

        cache.Remove(key);
        result = cache.Get(key);
        Assert.Null(result);
    }

    [Fact(Skip = SkipReason)]
    public void SetNullValueThrows()
    {
        var cache = RedisTestConfig.CreateCacheInstance(GetType().Name);
        byte[] value = null;
        string key = "myKey";

        Assert.Throws<ArgumentNullException>(() => cache.Set(key, value));
    }
}
