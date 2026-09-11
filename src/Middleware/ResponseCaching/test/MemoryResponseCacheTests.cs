// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.AspNetCore.ResponseCaching.Tests;

public class MemoryResponseCacheTests
{
    [Theory]
    [InlineData("a", 6)]
    [InlineData("\u00E9", 6)]
    [InlineData("\U0001F600", 8)]
    public void Set_CachedResponse_AccountsForKeySize(string key, long estimatedSize)
    {
        var entry = new CachedResponse
        {
            Headers = new HeaderDictionary(),
            Body = new CachedResponseBody([], 0)
        };
        var exactSizeCache = CreateCache(estimatedSize);
        var undersizedCache = CreateCache(estimatedSize - 1);

        exactSizeCache.Set(key, entry, TimeSpan.FromMinutes(1));
        undersizedCache.Set(key, entry, TimeSpan.FromMinutes(1));

        Assert.NotNull(exactSizeCache.Get(key));
        Assert.Null(undersizedCache.Get(key));
    }

    [Theory]
    [InlineData("a", 2)]
    [InlineData("\u00E9", 2)]
    [InlineData("\U0001F600", 4)]
    public void Set_CachedVaryByRules_AccountsForKeySize(string key, long estimatedSize)
    {
        var entry = new CachedVaryByRules();
        var exactSizeCache = CreateCache(estimatedSize);
        var undersizedCache = CreateCache(estimatedSize - 1);

        exactSizeCache.Set(key, entry, TimeSpan.FromMinutes(1));
        undersizedCache.Set(key, entry, TimeSpan.FromMinutes(1));

        Assert.NotNull(exactSizeCache.Get(key));
        Assert.Null(undersizedCache.Get(key));
    }

    private static MemoryResponseCache CreateCache(long sizeLimit)
        => new(new MemoryCache(new MemoryCacheOptions { SizeLimit = sizeLimit }));
}
