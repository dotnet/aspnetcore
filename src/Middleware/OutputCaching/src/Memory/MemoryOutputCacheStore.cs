// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.AspNetCore.OutputCaching.Memory;

internal sealed class MemoryOutputCacheStore : IOutputCacheStore
{
    private readonly MemoryCache _cache;
    private readonly Dictionary<string, HashSet<ValueTuple<string,Guid>>> _taggedEntries = new();
    private readonly object _tagsLock = new();

    internal MemoryOutputCacheStore(MemoryCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);

        _cache = cache;
    }

    // For testing
    internal Dictionary<string, HashSet<string>> TaggedEntries => _taggedEntries.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(t => t.Item1).ToHashSet());

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
                        foreach (var tuple in keys)
                        {
                            _cache.Remove(tuple.Item1);
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

        var entryId = Guid.NewGuid();

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
                        keys = new HashSet<ValueTuple<string, Guid>>();
                        _taggedEntries[tag] = keys;
                    }

                    Debug.Assert(keys != null);

                    keys.Add(ValueTuple.Create(key, entryId));
                }

                SetEntry(key, value, tags, validFor, entryId);
            }
        }
        else
        {
            SetEntry(key, value, tags, validFor, entryId);
        }

        return ValueTask.CompletedTask;
    }

    private void SetEntry(string key, byte[] value, string[]? tags, TimeSpan validFor, Guid entryId)
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
            options.RegisterPostEvictionCallback(RemoveFromTags, ValueTuple.Create(tags, entryId));
        }

        _cache.Set(key, value, options);
    }

    void RemoveFromTags(object key, object? value, EvictionReason reason, object? state)
    {
        Debug.Assert(state != null);

        var stateTuple = (ValueTuple<string[], Guid>) state;
        string[] tags = stateTuple.Item1;
        Guid entryId = stateTuple.Item2;

        Debug.Assert(tags != null);
        Debug.Assert(tags.Length > 0);
        Debug.Assert(key is string);

        lock (_tagsLock)
        {
            foreach (var tag in tags)
            {
                if (_taggedEntries.TryGetValue(tag, out var tagged))
                {
                    tagged.Remove(ValueTuple.Create((string) key, entryId));

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
