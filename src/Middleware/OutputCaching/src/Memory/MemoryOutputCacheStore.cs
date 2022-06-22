// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.AspNetCore.OutputCaching.Memory;

internal sealed class MemoryOutputCacheStore : IOutputCacheStore
{
    private readonly IMemoryCache _cache;
    private readonly Dictionary<string, HashSet<string>> _taggedEntries = new();
    private readonly object _tagsLock = new();

    internal MemoryOutputCacheStore(IMemoryCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);

        _cache = cache;
    }

    public ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tag);

        lock (_tagsLock)
        {
            if (_taggedEntries.TryGetValue(tag, out var keys))
            {
                foreach (var key in keys)
                {
                    _cache.Remove(key);
                }

                _taggedEntries.Remove(tag);
            }
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);

        var entry = _cache.Get(key) as byte[];
        return ValueTask.FromResult(entry);
    }

    /// <inheritdoc />
    public ValueTask SetAsync(string key, byte[] value, string[]? tags, TimeSpan validFor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        if (tags != null)
        {
            // Lock with SetEntry() to prevent EvictByTagAsync() from trying to remove a tag whose entry hasn't been added yet.
            // It might be acceptable to not lock SetEntry() since in this case Remove(key) would just no-op and the user retry to evict.

            lock (_tagsLock)
            {
                foreach (var tag in tags)
                {
                    if (!_taggedEntries.TryGetValue(tag, out var keys))
                    {
                        keys = new HashSet<string>();
                        _taggedEntries[tag] = keys;
                    }

                    keys.Add(key);
                }

                SetEntry();
            }
        }
        else
        {
            SetEntry();
        }

        void SetEntry()
        {
            _cache.Set(
            key,
            value,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = validFor,
                Size = value.Length
            });
        }

        return ValueTask.CompletedTask;
    }
}
