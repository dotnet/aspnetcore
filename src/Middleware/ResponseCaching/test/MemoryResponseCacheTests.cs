// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

#nullable enable

namespace Microsoft.AspNetCore.ResponseCaching.Tests;

public class MemoryResponseCacheTests
{
    private sealed class TestCacheEntry : ICacheEntry
    {
        public TestCacheEntry(object key)
        {
            Key = key;
        }

        public object Key { get; }
        public object? Value { get; set; }
        public DateTimeOffset? AbsoluteExpiration { get; set; }
        public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
        public TimeSpan? SlidingExpiration { get; set; }
        public IList<IChangeToken> ExpirationTokens { get; } = new List<IChangeToken>();
        public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; } = new List<PostEvictionCallbackRegistration>();
        public CacheItemPriority Priority { get; set; }
        public long? Size { get; set; }

        public void Dispose() { }
    }

    private sealed class TestMemoryCache : IMemoryCache
    {
        public TestCacheEntry? LastCreatedEntry { get; private set; }

        public ICacheEntry CreateEntry(object key)
        {
            ArgumentNullException.ThrowIfNull(key);
            LastCreatedEntry = new TestCacheEntry(key);
            return LastCreatedEntry;
        }

        public void Remove(object key) { }

        public bool TryGetValue(object key, out object? value)
        {
            value = null;
            return false;
        }

        public void Dispose() { }
    }

    private sealed class DummyResponseCacheEntry : IResponseCacheEntry
    {
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("key")]
    [InlineData("a-much-longer-cache-key-string-for-testing")]
    public void Set_CachedResponse_IncludesKeyByteContentInSize(string key)
    {
        var stubCache = new TestMemoryCache();
        var cache = new MemoryResponseCache(stubCache);
        var response = new CachedResponse
        {
            StatusCode = StatusCodes.Status200OK,
            Headers = new HeaderDictionary
            {
                { "Content-Type", "text/plain" },
                { "Custom-Header", "CustomValue" }
            },
            Body = new CachedResponseBody(new List<byte[]> { new byte[64] }, 64)
        };

        var expectedValueSize = CacheEntryHelpers.EstimateCachedResponseSize(response);
        var expectedKeyBytes = (long)key.Length * sizeof(char);

        cache.Set(key, response, TimeSpan.FromMinutes(1));

        Assert.NotNull(stubCache.LastCreatedEntry);
        Assert.Equal(key, stubCache.LastCreatedEntry.Key);
        Assert.Equal(expectedValueSize + expectedKeyBytes, stubCache.LastCreatedEntry.Size);
    }

    [Theory]
    [InlineData("")]
    [InlineData("vary-key")]
    [InlineData("vary-key-with-a-longer-name-to-verify-accounting")]
    public void Set_CachedVaryByRules_IncludesKeyByteContentInSize(string key)
    {
        var stubCache = new TestMemoryCache();
        var cache = new MemoryResponseCache(stubCache);
        var rules = new CachedVaryByRules
        {
            VaryByKeyPrefix = "prefix",
            Headers = new StringValues(new[] { "Accept", "Accept-Encoding" }),
            QueryKeys = new StringValues(new[] { "q", "page" })
        };

        var expectedValueSize = CacheEntryHelpers.EstimateCachedVaryByRulesySize(rules);
        var expectedKeyBytes = (long)key.Length * sizeof(char);

        cache.Set(key, rules, TimeSpan.FromMinutes(1));

        Assert.NotNull(stubCache.LastCreatedEntry);
        Assert.Equal(key, stubCache.LastCreatedEntry.Key);
        Assert.Equal(expectedValueSize + expectedKeyBytes, stubCache.LastCreatedEntry.Size);
    }

    [Fact]
    public void Set_UTF16CharacterHandling_UsesCodeUnitsWithoutTranscoding()
    {
        var stubCache = new TestMemoryCache();
        var cache = new MemoryResponseCache(stubCache);
        var response = new CachedResponse
        {
            StatusCode = StatusCodes.Status200OK,
            Headers = new HeaderDictionary(),
            Body = new CachedResponseBody(new List<byte[]> { new byte[10] }, 10)
        };
        var expectedValueSize = CacheEntryHelpers.EstimateCachedResponseSize(response);

        // 1. ASCII: 5 characters = 5 UTF-16 code units = 10 bytes
        var asciiKey = "hello";
        cache.Set(asciiKey, response, TimeSpan.FromMinutes(1));
        Assert.Equal(expectedValueSize + (5 * sizeof(char)), stubCache.LastCreatedEntry!.Size);

        // 2. BMP (Basic Multilingual Plane): "\u4e2d\u6587" (Chinese characters) = 2 characters = 4 bytes
        var bmpKey = "\u4e2d\u6587";
        Assert.Equal(2, bmpKey.Length);
        cache.Set(bmpKey, response, TimeSpan.FromMinutes(1));
        Assert.Equal(expectedValueSize + 4, stubCache.LastCreatedEntry!.Size);

        // 3. Surrogate pair emoji: "🚀" (\uD83D\uDE80) = 2 code units = 4 bytes in UTF-16
        var emojiKey = "🚀";
        Assert.Equal(2, emojiKey.Length);
        cache.Set(emojiKey, response, TimeSpan.FromMinutes(1));
        Assert.Equal(expectedValueSize + 4, stubCache.LastCreatedEntry!.Size);

        // 4. Multiple surrogate pairs: "🚀🎉" = 4 code units = 8 bytes in UTF-16
        var multiEmojiKey = "🚀🎉";
        Assert.Equal(4, multiEmojiKey.Length);
        cache.Set(multiEmojiKey, response, TimeSpan.FromMinutes(1));
        Assert.Equal(expectedValueSize + 8, stubCache.LastCreatedEntry!.Size);
    }

    [Fact]
    public void Set_EmptyKey_SizeMatchesValueEstimateAlone()
    {
        var stubCache = new TestMemoryCache();
        var cache = new MemoryResponseCache(stubCache);
        var response = new CachedResponse
        {
            StatusCode = StatusCodes.Status200OK,
            Headers = new HeaderDictionary(),
            Body = new CachedResponseBody(new List<byte[]> { new byte[32] }, 32)
        };

        var expectedValueSize = CacheEntryHelpers.EstimateCachedResponseSize(response);
        cache.Set("", response, TimeSpan.FromMinutes(1));

        Assert.NotNull(stubCache.LastCreatedEntry);
        Assert.Equal(expectedValueSize, stubCache.LastCreatedEntry.Size);
    }

    [Fact]
    public void Set_NullKey_ThrowsArgumentNullExceptionFromBackingCache()
    {
        var stubCache = new TestMemoryCache();
        var cacheWithStub = new MemoryResponseCache(stubCache);
        var response = new CachedResponse
        {
            StatusCode = StatusCodes.Status200OK,
            Headers = new HeaderDictionary(),
            Body = new CachedResponseBody(new List<byte[]>(), 0)
        };

        // Stub cache verification
        Assert.Throws<ArgumentNullException>(() => cacheWithStub.Set(null!, response, TimeSpan.FromMinutes(1)));

        // Real MemoryCache verification
        using var realMemoryCache = new MemoryCache(new MemoryCacheOptions());
        var cacheWithReal = new MemoryResponseCache(realMemoryCache);
        Assert.Throws<ArgumentNullException>(() => cacheWithReal.Set(null!, response, TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void Set_FallbackEntry_ChargesFallbackEstimatePlusKeyBytes()
    {
        var stubCache = new TestMemoryCache();
        var cache = new MemoryResponseCache(stubCache);
        var dummyEntry = new DummyResponseCacheEntry();
        var key = "fallback-test-key";
        var expectedKeyBytes = (long)key.Length * sizeof(char);

        cache.Set(key, dummyEntry, TimeSpan.FromMinutes(1));

        Assert.NotNull(stubCache.LastCreatedEntry);
        // Fallback estimate for non-CachedVaryByRules is EstimateCachedVaryByRulesySize(null) == 0
        Assert.Equal(expectedKeyBytes, stubCache.LastCreatedEntry.Size);
    }

    [Fact]
    public void Set_CheckedArithmeticOverflow_ThrowsOverflowException()
    {
        var stubCache = new TestMemoryCache();
        var cache = new MemoryResponseCache(stubCache);

        // Zero-allocation overflow: CachedResponseBody with length near long.MaxValue
        // EstimateCachedResponseSize computes: sizeof(int) [4] + Body.Length
        // With Body.Length = long.MaxValue - 10, EstimateCachedResponseSize returns long.MaxValue - 6
        var response = new CachedResponse
        {
            StatusCode = StatusCodes.Status200OK,
            Headers = new HeaderDictionary(),
            Body = new CachedResponseBody(new List<byte[]>(), long.MaxValue - 10)
        };

        // Key length 10 = 20 bytes. (long.MaxValue - 6) + 20 overflows long.MaxValue
        var key = new string('x', 10);

        Assert.Throws<OverflowException>(() => cache.Set(key, response, TimeSpan.FromMinutes(1)));
    }
}
