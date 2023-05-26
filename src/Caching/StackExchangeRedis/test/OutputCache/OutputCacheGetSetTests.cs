// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET7_0_OR_GREATER

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
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
#if DEBUG
        if (_cache is RedisOutputCacheStore real)
        {
            Log.WriteLine(await real.GetConfigurationInfoAsync().ConfigureAwait(false));
        }
#endif
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
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task SetStoresValueWithPrefixAndTimeout(bool useReadOnlySequence, bool withTags)
    {
        var cache = await Cache().ConfigureAwait(false);
        var key = Guid.NewGuid().ToString();
        byte[] storedValue = new byte[1017];
        Random.Shared.NextBytes(storedValue);
        RedisKey underlyingKey = "TestPrefix__MSOCV_" + key;

        // pre-check
        await _fixture.Database.KeyDeleteAsync(new RedisKey[] { "TestPrefix__MSOCT", "TestPrefix__MSOCT_tagA", "TestPrefix__MSOCT_tagB" });
        var timeout = await _fixture.Database.KeyTimeToLiveAsync(underlyingKey);
        Assert.Null(timeout); // means doesn't exist
        Assert.False(await _fixture.Database.KeyExistsAsync("TestPrefix__MSOCT"));
        Assert.False(await _fixture.Database.KeyExistsAsync("TestPrefix__MSOCT_tagA"));
        Assert.False(await _fixture.Database.KeyExistsAsync("TestPrefix__MSOCT_tagB"));

        // act
        var actTime = DateTime.UtcNow;
        var ttl = TimeSpan.FromSeconds(30);
        var tags = withTags ? new[] { "tagA", "tagB" } : null;
        if (useReadOnlySequence)
        {
            await cache.SetAsync(key, new ReadOnlySequence<byte>(storedValue), tags, ttl, CancellationToken.None);
        }
        else
        {
            await cache.SetAsync(key, storedValue, tags, ttl, CancellationToken.None);
        }

        // validate via redis direct
        timeout = await _fixture.Database.KeyTimeToLiveAsync(underlyingKey);
        Assert.NotNull(timeout); // means exists
        var seconds = timeout.Value.TotalSeconds;
        Assert.True(seconds >= 28 && seconds <= 32, "timeout should be in range");
        var redisValue = (byte[])(await _fixture.Database.StringGetAsync(underlyingKey));
        Assert.True(((ReadOnlySpan<byte>)storedValue).SequenceEqual(redisValue), "payload should match");

        double expected = (long)((actTime + ttl) - DateTime.UnixEpoch).TotalMilliseconds;
        if (withTags)
        {
            // we expect the tag structure to now exist, with the scores within a bit of a second
            const double Tolerance = 100.0;
            Assert.Equal((await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT", "tagA")).Value, expected, Tolerance);
            Assert.Equal((await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT", "tagB")).Value, expected, Tolerance);
            Assert.Equal((await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_tagA", key)).Value, expected, Tolerance);
            Assert.Equal((await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_tagB", key)).Value, expected, Tolerance);
        }
        else
        {
            // we do *not* expect the tag structure to exist
            Assert.False(await _fixture.Database.KeyExistsAsync("TestPrefix__MSOCT"));
            Assert.False(await _fixture.Database.KeyExistsAsync("TestPrefix__MSOCT_tagA"));
            Assert.False(await _fixture.Database.KeyExistsAsync("TestPrefix__MSOCT_tagB"));
        }
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

    [Fact]
    public async Task TagScoreWorksWithGreaterThan()
    {
        // store some data
        var cache = await Cache().ConfigureAwait(false);
        byte[] storedValue = new byte[1017];
        Random.Shared.NextBytes(storedValue);
        var tags = new[] { "gtonly" };
        await _fixture.Database.KeyDeleteAsync("TestPrefix__MSOCT"); // start from nil state

        await cache.SetAsync(Guid.NewGuid().ToString(), storedValue, tags, TimeSpan.FromSeconds(30), CancellationToken.None);
        var originalScore = await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT", "gtonly");
        Assert.NotNull(originalScore);

        // now store something with a shorter ttl; the score should not change
        await cache.SetAsync(Guid.NewGuid().ToString(), storedValue, tags, TimeSpan.FromSeconds(15), CancellationToken.None);
        var newScore = await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT", "gtonly");
        Assert.NotNull(newScore);
        Assert.Equal(originalScore, newScore);

        // now store something with a longer ttl; the score should increase
        await cache.SetAsync(Guid.NewGuid().ToString(), storedValue, tags, TimeSpan.FromSeconds(45), CancellationToken.None);
        newScore = await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT", "gtonly");
        Assert.NotNull(newScore);
        Assert.True(newScore > originalScore, "should increase");
    }

    [Fact]
    public async Task CanEvictByTag()
    {
        // store some data
        var cache = await Cache().ConfigureAwait(false);
        byte[] storedValue = new byte[1017];
        Random.Shared.NextBytes(storedValue);
        var ttl = TimeSpan.FromSeconds(30);

        var noTags = Guid.NewGuid().ToString();
        await cache.SetAsync(noTags, storedValue, null, ttl, CancellationToken.None);

        var foo = Guid.NewGuid().ToString();
        await cache.SetAsync(foo, storedValue, new[] {"foo"}, ttl, CancellationToken.None);

        var bar = Guid.NewGuid().ToString();
        await cache.SetAsync(bar, storedValue, new[] { "bar" }, ttl, CancellationToken.None);

        var fooBar = Guid.NewGuid().ToString();
        await cache.SetAsync(fooBar, storedValue, new[] { "foo", "bar" }, ttl, CancellationToken.None);

        // assert prior state
        Assert.NotNull(await cache.GetAsync(noTags, CancellationToken.None));
        Assert.NotNull(await cache.GetAsync(foo, CancellationToken.None));
        Assert.NotNull(await cache.GetAsync(bar, CancellationToken.None));
        Assert.NotNull(await cache.GetAsync(fooBar, CancellationToken.None));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_foo", noTags));
        Assert.NotNull(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_foo", foo));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_foo", bar));
        Assert.NotNull(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_foo", fooBar));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_bar", noTags));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_bar", foo));
        Assert.NotNull(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_bar", bar));
        Assert.NotNull(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_bar", fooBar));

        // act
        for (int i = 0; i < 2; i++) // loop is to ensure no oddity when working on tags that *don't* have entries
        {
            await cache.EvictByTagAsync("foo", CancellationToken.None);
            Assert.NotNull(await cache.GetAsync(noTags, CancellationToken.None));
            Assert.Null(await cache.GetAsync(foo, CancellationToken.None));
            Assert.NotNull(await cache.GetAsync(bar, CancellationToken.None));
            Assert.Null(await cache.GetAsync(fooBar, CancellationToken.None));
        }
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_foo", noTags));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_foo", foo));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_foo", bar));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_foo", fooBar));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_bar", noTags));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_bar", foo));
        Assert.NotNull(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_bar", bar));
        Assert.NotNull(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_bar", fooBar));

        for (int i = 0; i < 2; i++) // loop is to ensure no oddity when working on tags that *don't* have entries
        {
            await cache.EvictByTagAsync("bar", CancellationToken.None);
            Assert.NotNull(await cache.GetAsync(noTags, CancellationToken.None));
            Assert.Null(await cache.GetAsync(foo, CancellationToken.None));
            Assert.Null(await cache.GetAsync(bar, CancellationToken.None));
            Assert.Null(await cache.GetAsync(fooBar, CancellationToken.None));
        }
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_foo", noTags));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_foo", foo));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_foo", bar));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_foo", fooBar));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_bar", noTags));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_bar", foo));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_bar", bar));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_bar", fooBar));

        // assert expected state
        Assert.NotNull(await cache.GetAsync(noTags, CancellationToken.None));
        Assert.Null(await cache.GetAsync(foo, CancellationToken.None));
        Assert.Null(await cache.GetAsync(bar, CancellationToken.None));
        Assert.Null(await cache.GetAsync(fooBar, CancellationToken.None));
    }
}

#endif
