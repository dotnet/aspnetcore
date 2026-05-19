// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.AspNetCore.ResponseCaching;

internal sealed class MemoryResponseCache : IResponseCache
{
    private readonly IMemoryCache _cache;

    internal MemoryResponseCache(IMemoryCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public IResponseCacheEntry? Get(string key)
    {
        var entry = _cache.Get(key);

        if (entry is MemoryCachedResponse memoryCachedResponse)
        {
            return new CachedResponse
            {
                Created = memoryCachedResponse.Created,
                StatusCode = memoryCachedResponse.StatusCode,
                Headers = memoryCachedResponse.Headers,
                Body = memoryCachedResponse.Body
            };
        }
        else
        {
            return entry as IResponseCacheEntry;
        }
    }

    public void Set(string key, IResponseCacheEntry entry, TimeSpan validFor)
    {
        if (entry is CachedResponse cachedResponse)
        {
            _cache.Set(
                key,
                new MemoryCachedResponse
                {
                    Created = cachedResponse.Created,
                    StatusCode = cachedResponse.StatusCode,
                    Headers = cachedResponse.Headers,
                    Body = cachedResponse.Body
                },
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = validFor,
                    Size = CacheEntryHelpers.EstimateCachedResponseSize(cachedResponse)
                });
        }
        else
        {
            _cache.Set(
                key,
                entry,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = validFor,
                    Size = CacheEntryHelpers.EstimateCachedVaryByRulesySize(entry as CachedVaryByRules)
                });
        }
    }
}
