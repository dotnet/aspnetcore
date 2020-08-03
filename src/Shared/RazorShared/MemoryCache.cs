// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    // We've created our own MemoryCache here, ideally we would use the one in Microsoft.Extensions.Caching.Memory,
    // but until we update O# that causes an Assembly load problem.
    internal class MemoryCache<TKey, TValue>
    {
        private static readonly int SizeLimit = 1500;

        protected IDictionary<TKey, CacheEntry> _dict;

        public MemoryCache()
        {
            _dict = new ConcurrentDictionary<TKey, CacheEntry>(concurrencyLevel: 2, capacity: SizeLimit);
        }

        public bool TryGetValue(TKey key, out TValue result)
        {
            var entryFound = _dict.TryGetValue(key, out var value);

            if (entryFound)
            {
                value.LastAccess = DateTime.UtcNow;
                result = value.Value;
            }
            else
            {
                result = default;
            }

            return entryFound;
        }

        public void Set(TKey key, TValue value)
        {
            if (_dict.Count >= SizeLimit)
            {
                Compact();
            }

            _dict.Add(key, new CacheEntry
            {
                LastAccess = DateTime.UtcNow,
                Value = value,
            });
        }

        protected virtual void Compact()
        {
            var kvps = _dict.OrderBy(x => x.Value.LastAccess);

            for (var i = 0; i < SizeLimit / 2; i++)
            {
                _dict.Remove(kvps.ElementAt(i).Key);
            }
        }

        protected class CacheEntry
        {
            public TValue Value { get; set; }

            public DateTime LastAccess { get; set; }
        }
    }
}
