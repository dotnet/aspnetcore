// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.AspNetCore.ResponseCaching.Tests;

public class MemoryResponseCacheTests
{
    [Theory]
    [InlineData("a")]
    [InlineData("\u00E9")]
    [InlineData("\U0001F600")]
    public void Set_CachedResponse_AccountsForKeySize(string key)
    {
        var entry = new CachedResponse
        {
            Headers = new HeaderDictionary { ["name"] = "value" },
            Body = new CachedResponseBody([[1, 2, 3]], 3)
        };
        var estimatedSize = CacheEntryHelpers.EstimateCachedResponseSize(entry) + (key.Length * sizeof(char));
        var exactSizeCache = CreateCache(estimatedSize);
        var undersizedCache = CreateCache(estimatedSize - 1);

        exactSizeCache.Set(key, entry, TimeSpan.FromMinutes(1));
        undersizedCache.Set(key, entry, TimeSpan.FromMinutes(1));

        Assert.NotNull(exactSizeCache.Get(key));
        Assert.Null(undersizedCache.Get(key));
    }

    [Theory]
    [InlineData("a")]
    [InlineData("\u00E9")]
    [InlineData("\U0001F600")]
    public void Set_CachedVaryByRules_AccountsForKeySize(string key)
    {
        var entry = new CachedVaryByRules
        {
            VaryByKeyPrefix = "prefix",
            Headers = "header",
            QueryKeys = "query"
        };
        var estimatedSize = CacheEntryHelpers.EstimateCachedVaryByRulesySize(entry) + (key.Length * sizeof(char));
        var exactSizeCache = CreateCache(estimatedSize);
        var undersizedCache = CreateCache(estimatedSize - 1);

        exactSizeCache.Set(key, entry, TimeSpan.FromMinutes(1));
        undersizedCache.Set(key, entry, TimeSpan.FromMinutes(1));

        Assert.NotNull(exactSizeCache.Get(key));
        Assert.Null(undersizedCache.Get(key));
    }

    [Fact]
    public void Set_EmptyKey_AccountsForValueOnly()
    {
        var entry = new CachedResponse
        {
            Headers = new HeaderDictionary(),
            Body = new CachedResponseBody([], 0)
        };
        var cache = CreateCache(CacheEntryHelpers.EstimateCachedResponseSize(entry));

        cache.Set(string.Empty, entry, TimeSpan.FromMinutes(1));

        Assert.NotNull(cache.Get(string.Empty));
    }

    [Fact]
    public void Set_NullKey_PreservesMemoryCacheRejection()
    {
        var cache = CreateCache(1);

        Assert.Throws<ArgumentNullException>(() => cache.Set(null!, new TestCacheEntry(), TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void Set_FallbackEntry_AccountsForKeySize()
    {
        var key = "key";
        var exactSizeCache = CreateCache(key.Length * sizeof(char));
        var undersizedCache = CreateCache((key.Length * sizeof(char)) - 1);

        exactSizeCache.Set(key, new TestCacheEntry(), TimeSpan.FromMinutes(1));
        undersizedCache.Set(key, new TestCacheEntry(), TimeSpan.FromMinutes(1));

        Assert.NotNull(exactSizeCache.Get(key));
        Assert.Null(undersizedCache.Get(key));
    }

    [Fact]
    public void Set_MixedEntries_ConsumeAggregateCapacity()
    {
        var responseKey = "response";
        var response = new CachedResponse
        {
            Headers = new HeaderDictionary(),
            Body = new CachedResponseBody([], 0)
        };
        var responseSize = CacheEntryHelpers.EstimateCachedResponseSize(response) + (responseKey.Length * sizeof(char));
        var rulesKey = "rules";
        var rules = new CachedVaryByRules();
        var rulesSize = CacheEntryHelpers.EstimateCachedVaryByRulesySize(rules) + (rulesKey.Length * sizeof(char));
        var cache = CreateCache(responseSize + rulesSize);

        cache.Set(responseKey, response, TimeSpan.FromMinutes(1));
        cache.Set(rulesKey, rules, TimeSpan.FromMinutes(1));
        cache.Set("additional", new CachedVaryByRules(), TimeSpan.FromMinutes(1));

        Assert.NotNull(cache.Get(responseKey));
        Assert.NotNull(cache.Get(rulesKey));
        Assert.Null(cache.Get("additional"));
    }

    [Fact]
    public void Set_ReplacingEntry_AccountsForOneEntryAndRemovesOversizedReplacement()
    {
        var key = "key";
        var response = new CachedResponse
        {
            Headers = new HeaderDictionary(),
            Body = new CachedResponseBody([], 0)
        };
        var responseSize = CacheEntryHelpers.EstimateCachedResponseSize(response) + (key.Length * sizeof(char));
        var cache = CreateCache(responseSize);

        cache.Set(key, new CachedVaryByRules(), TimeSpan.FromMinutes(1));
        cache.Set(key, response, TimeSpan.FromMinutes(1));

        Assert.IsType<CachedResponse>(cache.Get(key));

        response.Body = new CachedResponseBody([[1]], 1);
        cache.Set(key, response, TimeSpan.FromMinutes(1));

        Assert.Null(cache.Get(key));
    }

    private static MemoryResponseCache CreateCache(long sizeLimit)
        => new(new MemoryCache(new MemoryCacheOptions { SizeLimit = sizeLimit }));

    private sealed class TestCacheEntry : IResponseCacheEntry;
}
