// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.AspNetCore.OutputCaching.Memory;

internal sealed class MemoryOutputCacheStore : IOutputCacheStore
{
    private readonly MemoryCache _cache;
    private readonly Dictionary<string, HashSet<string>> _taggedEntries = new();
    private readonly object _tagsLock = new();

    internal MemoryOutputCacheStore(MemoryCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);

        _cache = cache;
    }

    // For testing
    internal Dictionary<string, HashSet<string>> TaggedEntries => _taggedEntries;

    public ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tag);

        lock (_tagsLock)
        {
            if (_taggedEntries.TryGetValue(tag, out var keys))
            {
                if (keys != null && keys.Count > 0)
                {
                    // If MemoryCache changed to run eviction callbacks inline in Remove, iterating over keys could throw
                    // To prevent allocating a copy of the keys we check if the eviction callback ran,
                    // and if it did we restart the loop.

                    var i = keys.Count;
                    while (i > 0)
                    {
                        var oldCount = keys.Count;
                        foreach (var key in keys)
                        {
                            _cache.Remove(key);
                            i--;
                            if (oldCount != keys.Count)
                            {
                                // eviction callback ran inline, we need to restart the loop to avoid
                                // "collection modified while iterating" errors
                                break;
                            }
                        }
                    }
                }
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
                    if (tag is null)
                    {
                        throw new ArgumentException(Resources.TagCannotBeNull);
                    }

                    if (!_taggedEntries.TryGetValue(tag, out var keys))
                    {
                        keys = new HashSet<string>();
                        _taggedEntries[tag] = keys;
                    }

                    Debug.Assert(keys != null);

                    keys.Add(key);
                }

                SetEntry(key, value, tags, validFor);
            }
        }
        else
        {
            SetEntry(key, value, tags, validFor);
        }

        return ValueTask.CompletedTask;
    }

    void SetEntry(string key, byte[] value, string[]? tags, TimeSpan validFor)
    {
        Debug.Assert(key != null);

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = validFor,
            Size = value.Length
        };

        if (tags != null && tags.Length > 0)
        {
            // Remove cache keys from tag lists when the entry is evicted
            options.RegisterPostEvictionCallback(RemoveFromTags, tags);
        }

        _cache.Set(key, value, options);
    }

    void RemoveFromTags(object key, object? value, EvictionReason reason, object? state)
    {
        var tags = state as string[];

        Debug.Assert(tags != null);
        Debug.Assert(tags.Length > 0);
        Debug.Assert(key is string);

        lock (_tagsLock)
        {
            foreach (var tag in tags)
            {
                if (_taggedEntries.TryGetValue(tag, out var tagged))
                {
                    tagged.Remove((string)key);

                    // Remove the collection if there is no more keys in it
                    if (tagged.Count == 0)
                    {
                        _taggedEntries.Remove(tag);
                    }
                }
            }
        }
    }
}
