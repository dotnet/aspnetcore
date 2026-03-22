// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class MemoryCacheComponentStore : ICacheComponentStore
{
    private readonly MemoryCache _cache;

    public MemoryCacheComponentStore()
    {
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 50_000_000,
        });
    }

    public bool TryGetValue(string key, out string? value)
    {
        return _cache.TryGetValue(key, out value);
    }

    public void Set(string key, string value, TimeSpan absoluteExpirationRelativeToNow)
    {
        var entryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow,
            Size = value.Length,
        };
        _cache.Set(key, value, entryOptions);
    }

    public void Dispose()
    {
        _cache.Dispose();
    }
}
