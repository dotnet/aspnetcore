// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OutputCaching.Memory;
using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.AspNetCore.OutputCaching.Tests;

public class MemoryOutputCacheStoreTests
{
    [Fact]
    public async Task StoreAndGetValue_Succeeds()
    {
        var store = new MemoryOutputCacheStore(new MemoryCache(new MemoryCacheOptions()));
        var value = "abc"u8.ToArray();
        var key = "abc";

        await store.SetAsync(key, value, null, TimeSpan.FromMinutes(1), default);

        var result = await store.GetAsync(key, default);

        Assert.Equal(value, result);
    }

    [Fact]
    public async Task StoreAndGetValue_TimesOut()
    {
        var testClock = new TestMemoryOptionsClock { UtcNow = DateTimeOffset.UtcNow };
        var store = new MemoryOutputCacheStore(new MemoryCache(new MemoryCacheOptions { Clock = testClock }));
        var value = "abc"u8.ToArray();
        var key = "abc";

        await store.SetAsync(key, value, null, TimeSpan.FromMilliseconds(5), default);
        testClock.Advance(TimeSpan.FromMilliseconds(10));

        var result = await store.GetAsync(key, default);

        Assert.Null(result);
    }

    [Fact]
    public async Task StoreNullKey_ThrowsException()
    {
        var store = new MemoryOutputCacheStore(new MemoryCache(new MemoryCacheOptions()));
        var value = "abc"u8.ToArray();
        string key = null;

        _ = await Assert.ThrowsAsync<ArgumentNullException>("key", () => store.SetAsync(key, value, null, TimeSpan.FromMilliseconds(5), default).AsTask());
    }

    [Fact]
    public async Task StoreNullValue_ThrowsException()
    {
        var store = new MemoryOutputCacheStore(new MemoryCache(new MemoryCacheOptions()));
        var value = default(byte[]);
        var key = "abc";

        _ = await Assert.ThrowsAsync<ArgumentNullException>("value", () => store.SetAsync(key, value, null, TimeSpan.FromMilliseconds(5), default).AsTask());
    }

    [Fact]
    public async Task EvictByTag_SingleTag_SingleEntry()
    {
        var testClock = new TestMemoryOptionsClock { UtcNow = DateTimeOffset.UtcNow };
        var store = new MemoryOutputCacheStore(new MemoryCache(new MemoryCacheOptions { Clock = testClock }));
        var value = "abc"u8.ToArray();
        var key = "abc";
        var tags = new string[] { "tag1" };

        await store.SetAsync(key, value, tags, TimeSpan.FromDays(1), default);
        await store.EvictByTagAsync("tag1", default);
        var result = await store.GetAsync(key, default);

        HashSet<string> tag1s;

        // Wait for the hashset to be removed as it happens on a separate thread

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        while (store.TaggedEntries.TryGetValue("tag1", out tag1s) && !cts.IsCancellationRequested)
        {
            await Task.Yield();
        }

        Assert.Null(result);
        Assert.Null(tag1s);
    }

    [Fact]
    public async Task EvictByTag_SingleTag_MultipleEntries()
    {
        var testClock = new TestMemoryOptionsClock { UtcNow = DateTimeOffset.UtcNow };
        var store = new MemoryOutputCacheStore(new MemoryCache(new MemoryCacheOptions { Clock = testClock }));
        var value = "abc"u8.ToArray();
        var key1 = "abc";
        var key2 = "def";
        var tags = new string[] { "tag1" };

        await store.SetAsync(key1, value, tags, TimeSpan.FromDays(1), default);
        await store.SetAsync(key2, value, tags, TimeSpan.FromDays(1), default);
        await store.EvictByTagAsync("tag1", default);
        var result1 = await store.GetAsync(key1, default);
        var result2 = await store.GetAsync(key2, default);

        Assert.Null(result1);
        Assert.Null(result2);
    }

    [Fact]
    public async Task EvictByTag_MultipleTags_SingleEntry()
    {
        var testClock = new TestMemoryOptionsClock { UtcNow = DateTimeOffset.UtcNow };
        var store = new MemoryOutputCacheStore(new MemoryCache(new MemoryCacheOptions { Clock = testClock }));
        var value = "abc"u8.ToArray();
        var key = "abc";
        var tags = new string[] { "tag1", "tag2" };

        await store.SetAsync(key, value, tags, TimeSpan.FromDays(1), default);
        await store.EvictByTagAsync("tag1", default);
        var result1 = await store.GetAsync(key, default);

        Assert.Null(result1);
    }

    [Fact]
    public async Task EvictByTag_MultipleTags_MultipleEntries()
    {
        var testClock = new TestMemoryOptionsClock { UtcNow = DateTimeOffset.UtcNow };
        var store = new MemoryOutputCacheStore(new MemoryCache(new MemoryCacheOptions { Clock = testClock }));
        var value = "abc"u8.ToArray();
        var key1 = "abc";
        var key2 = "def";
        var tags1 = new string[] { "tag1", "tag2" };
        var tags2 = new string[] { "tag2", "tag3" };

        await store.SetAsync(key1, value, tags1, TimeSpan.FromDays(1), default);
        await store.SetAsync(key2, value, tags2, TimeSpan.FromDays(1), default);
        await store.EvictByTagAsync("tag1", default);

        var result1 = await store.GetAsync(key1, default);
        var result2 = await store.GetAsync(key2, default);

        Assert.Null(result1);
        Assert.NotNull(result2);

        await store.EvictByTagAsync("tag3", default);

        result1 = await store.GetAsync(key1, default);
        result2 = await store.GetAsync(key2, default);

        Assert.Null(result1);
        Assert.Null(result2);
    }

    [Fact]
    public async Task ExpiredEntries_AreRemovedFromTags()
    {
        var testClock = new TestMemoryOptionsClock { UtcNow = DateTimeOffset.UtcNow };
        var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1000, Clock = testClock, ExpirationScanFrequency = TimeSpan.FromMilliseconds(1) });
        var store = new MemoryOutputCacheStore(cache);
        var value = "abc"u8.ToArray();

        await store.SetAsync("a", value, new[] { "tag1" }, TimeSpan.FromMilliseconds(5), default);
        await store.SetAsync("b", value, new[] { "tag1", "tag2" }, TimeSpan.FromMilliseconds(5), default);
        await store.SetAsync("c", value, new[] { "tag2" }, TimeSpan.FromMilliseconds(20), default);

        testClock.Advance(TimeSpan.FromMilliseconds(10));

        // Background expiration checks are triggered by misc cache activity.
        _ = cache.Get("a");

        var resulta = await store.GetAsync("a", default);
        var resultb = await store.GetAsync("b", default);
        var resultc = await store.GetAsync("c", default);

        Assert.Null(resulta);
        Assert.Null(resultb);
        Assert.NotNull(resultc);

        HashSet<string> tag1s, tag2s;

        // Wait for the hashset to be removed as it happens on a separate thread

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        while (store.TaggedEntries.TryGetValue("tag1", out tag1s) && !cts.IsCancellationRequested)
        {
            await Task.Yield();
        }

        while (store.TaggedEntries.TryGetValue("tag2", out tag2s) && tag2s.Count != 1 && !cts.IsCancellationRequested)
        {
            await Task.Yield();
        }

        Assert.Null(tag1s);
        Assert.Single(tag2s);
    }

    [Theory]
    [InlineData(null)]
    public async Task Store_Throws_OnInvalidTag(string tag)
    {
        var store = new MemoryOutputCacheStore(new MemoryCache(new MemoryCacheOptions()));
        var value = "abc"u8.ToArray();
        var key = "abc";

        await Assert.ThrowsAsync<ArgumentException>(async () => await store.SetAsync(key, value, new string[] { tag }, TimeSpan.FromMinutes(1), default));
    }

    private class TestMemoryOptionsClock : Extensions.Internal.ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; }
        public void Advance(TimeSpan duration)
        {
            UtcNow += duration;
        }
    }
}
