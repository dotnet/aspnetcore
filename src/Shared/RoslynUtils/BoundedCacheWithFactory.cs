// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Analyzers.Infrastructure;

// This type is copied from https://github.com/dotnet/roslyn-analyzers/blob/9b58ec3ad33353d1a523cda8c4be38eaefc80ad8/src/Utilities/Compiler/BoundedCacheWithFactory.cs

/// <summary>
/// Provides bounded cache for analyzers.
/// Acts as a good alternative to <see cref="System.Runtime.CompilerServices.ConditionalWeakTable{TKey, TValue}"/>
/// when the cached value has a cyclic reference to the key preventing early garbage collection of entries.
/// </summary>
internal class BoundedCacheWithFactory<TKey, TValue>
    where TKey : class
{
    // Bounded weak reference cache.
    // Size 5 is an arbitrarily chosen bound, which can be tuned in future as required.
    private readonly List<WeakReference<Entry?>> _weakReferencedEntries = new()
        {
            new WeakReference<Entry?>(null),
            new WeakReference<Entry?>(null),
            new WeakReference<Entry?>(null),
            new WeakReference<Entry?>(null),
            new WeakReference<Entry?>(null),
        };

    public TValue GetOrCreateValue(TKey key, Func<TKey, TValue> valueFactory)
    {
        lock (_weakReferencedEntries)
        {
            var indexToSetTarget = -1;
            for (var i = 0; i < _weakReferencedEntries.Count; i++)
            {
                var weakReferencedEntry = _weakReferencedEntries[i];
                if (!weakReferencedEntry.TryGetTarget(out var cachedEntry) ||
                    cachedEntry == null)
                {
                    if (indexToSetTarget == -1)
                    {
                        indexToSetTarget = i;
                    }

                    continue;
                }

                if (Equals(cachedEntry.Key, key))
                {
                    // Move the cache hit item to the end of the list
                    // so it would be least likely to be evicted on next cache miss.
                    _weakReferencedEntries.RemoveAt(i);
                    _weakReferencedEntries.Add(weakReferencedEntry);
                    return cachedEntry.Value;
                }
            }

            if (indexToSetTarget == -1)
            {
                indexToSetTarget = 0;
            }

            var newEntry = new Entry(key, valueFactory(key));
            _weakReferencedEntries[indexToSetTarget].SetTarget(newEntry);
            return newEntry.Value;
        }
    }

    private sealed class Entry
    {
        public Entry(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        public TKey Key { get; }

        public TValue Value { get; }
    }
}
