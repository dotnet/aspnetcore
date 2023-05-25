// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET7_0_OR_GREATER

using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OutputCaching;
using StackExchange.Redis;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Caching.StackExchangeRedis;

public class OutputCacheGetSetTests : IClassFixture<RedisConnectionFixture>
{
    private readonly IOutputCacheStore _cache;
    private readonly RedisConnectionFixture _fixture;
    private readonly ITestOutputHelper Log;

    public OutputCacheGetSetTests(RedisConnectionFixture connection, ITestOutputHelper log)
    {
        _fixture = connection;
        _cache = new RedisOutputCacheStore(new RedisCacheOptions
        {
            ConnectionMultiplexerFactory = () => Task.FromResult(_fixture.Connection),
            InstanceName = "TestPrefix",
        });
        Log = log;
    }

    private async ValueTask<IOutputCacheStore> Cache()
    {
        if (_cache is RedisOutputCacheStore real)
        {
            Log.WriteLine(await real.GetConfigurationInfo().ConfigureAwait(false));
        }
        return _cache;
    }

    [Fact]
    public async Task GetMissingKeyReturnsNull()
    {
        var cache = await Cache().ConfigureAwait(false);
        var result = await cache.GetAsync("non-existent-key", CancellationToken.None);
        Assert.Null(result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetStoresValueWithPrefixAndTimeout(bool useReadOnlySequence)
    {
        var cache = await Cache().ConfigureAwait(false);
        var key = Guid.NewGuid().ToString();
        byte[] storedValue = new byte[1017];
        Random.Shared.NextBytes(storedValue);
        RedisKey underlyingKey = "TestPrefix__MSOCV_" + key;

        // pre-check
        var timeout = await _fixture.Database.KeyTimeToLiveAsync(underlyingKey);
        Assert.Null(timeout); // means doesn't exist

        // act
        if (useReadOnlySequence)
        {
            await cache.SetAsync(key, new ReadOnlySequence<byte>(storedValue), null, TimeSpan.FromSeconds(30), CancellationToken.None);
        }
        else
        {
            await cache.SetAsync(key, storedValue, null, TimeSpan.FromSeconds(30), CancellationToken.None);
        }

        // validate via redis direct
        timeout = await _fixture.Database.KeyTimeToLiveAsync(underlyingKey);
        Assert.NotNull(timeout); // means exists
        var seconds = timeout.Value.TotalSeconds;
        Assert.True(seconds >= 28 && seconds <= 32, "timeout should be in range");
        var redisValue = (byte[])(await _fixture.Database.StringGetAsync(underlyingKey));
        Assert.True(((ReadOnlySpan<byte>)storedValue).SequenceEqual(redisValue), "payload should match");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CanFetchStoredValue(bool useReadOnlySequence)
    {
        var cache = await Cache().ConfigureAwait(false);
        var key = Guid.NewGuid().ToString();
        byte[] storedValue = new byte[1017];
        Random.Shared.NextBytes(storedValue);

        // pre-check
        var fetchedValue = await cache.GetAsync(key, CancellationToken.None);
        Assert.Null(fetchedValue);

        // store and fetch via service
        if (useReadOnlySequence)
        {
            await cache.SetAsync(key, new ReadOnlySequence<byte>(storedValue), null, TimeSpan.FromSeconds(30), CancellationToken.None);
        }
        else
        {
            await cache.SetAsync(key, storedValue, null, TimeSpan.FromSeconds(30), CancellationToken.None);
        }
        fetchedValue = await cache.GetAsync(key, CancellationToken.None);
        Assert.NotNull(fetchedValue);

        Assert.True(((ReadOnlySpan<byte>)storedValue).SequenceEqual(fetchedValue), "payload should match");
    }
}

#endif
