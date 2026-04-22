// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class MemoryCacheBoundaryStore : CacheBoundaryStore
{
    private readonly MemoryCache _cache;

    public MemoryCacheBoundaryStore(IOptions<RazorComponentsServiceOptions> options)
    {
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = options.Value.CacheBoundarySizeLimit,
        });
    }

    public override string? Get(string key)
    {
        _cache.TryGetValue(key, out string? cached);
        return cached;
    }

    public override void Set(string key, string json, CacheStoreOptions options = default)
    {
        var entryOptions = new MemoryCacheEntryOptions
        {
            Size = json.Length * sizeof(char),
        };

        if (options.ExpiresSliding.HasValue)
        {
            entryOptions.SlidingExpiration = options.ExpiresSliding.Value;
        }

        if (options.ExpiresOn.HasValue)
        {
            entryOptions.AbsoluteExpiration = options.ExpiresOn.Value;
        }
        else
        {
            entryOptions.AbsoluteExpirationRelativeToNow = options.ExpiresAfter ?? DefaultExpiration;
        }

        if (options.Priority.HasValue)
        {
            entryOptions.Priority = options.Priority.Value;
        }

        _cache.Set(key, json, entryOptions);
    }

    public override void Clear()
    {
        _cache.Clear();
    }

    public override void Dispose()
    {
        _cache.Dispose();
    }
}
