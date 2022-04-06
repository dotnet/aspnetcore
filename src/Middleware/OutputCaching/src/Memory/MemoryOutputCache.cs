// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.AspNetCore.OutputCaching.Memory;

internal sealed class MemoryOutputCacheStore : IOutputCacheStore
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, HashSet<string>> _taggedEntries = new();

    internal MemoryOutputCacheStore(IMemoryCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache, nameof(cache));

        _cache = cache;
    }

    public ValueTask EvictByTagAsync(string tag)
    {
        if (_taggedEntries.TryGetValue(tag, out var keys))
        {
            foreach (var key in keys)
            {
                _cache.Remove(key);
            }
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<OutputCacheEntry?> GetAsync(string key)
    {
        var entry = _cache.Get(key);

        if (entry is MemoryCachedResponse memoryCachedResponse)
        {
            var outputCacheEntry = new OutputCacheEntry
            {
                Created = memoryCachedResponse.Created,
                StatusCode = memoryCachedResponse.StatusCode,
                Headers = memoryCachedResponse.Headers,
                Body = memoryCachedResponse.Body,
                Tags = memoryCachedResponse.Tags.ToArray()
            };

            return ValueTask.FromResult<OutputCacheEntry?>(outputCacheEntry);
        }

        return ValueTask.FromResult(default(OutputCacheEntry));
    }

    public ValueTask SetAsync(string key, OutputCacheEntry cachedResponse, TimeSpan validFor)
    {
        if (cachedResponse.Tags != null)
        {
            foreach (var tag in cachedResponse.Tags)
            {
                var keys = _taggedEntries.GetOrAdd(tag, _ => new HashSet<string>());

                // Copy the list of tags to prevent locking

                var local = new HashSet<string>(keys)
                {
                    key
                };

                _taggedEntries[tag] = local;
            }
        }

        _cache.Set(
            key,
            new MemoryCachedResponse
            {
                Created = cachedResponse.Created,
                StatusCode = cachedResponse.StatusCode,
                Headers = cachedResponse.Headers,
                Body = cachedResponse.Body,
                Tags = cachedResponse.Tags ?? Array.Empty<string>()
            },
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = validFor,
                Size = CacheEntryHelpers.EstimateCachedResponseSize(cachedResponse)
            });

        return ValueTask.CompletedTask;
    }
}
