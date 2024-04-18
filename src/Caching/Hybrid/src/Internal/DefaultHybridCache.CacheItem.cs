// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

partial class DefaultHybridCache
{
    internal abstract class CacheItem
    {
        internal static readonly PostEvictionDelegate OnEviction = (key, value, reason, state) =>
        {
            if (value is CacheItem item)
            {
                // in reality we only set this up for mutable cache items, as a mechanism
                // to recycle the buffers
                item.Release();
            }
        };

        public virtual void Release() { } // for recycling purposes

        public virtual bool NeedsEvictionCallback => false; // do we need to call Release when evicted?

        public abstract bool TryReserveBuffer(out BufferChunk buffer);
    }

    internal abstract class CacheItem<T> : CacheItem
    {
        public abstract bool TryGetValue(out T value);

        public T GetValue()
        {
            if (!TryGetValue(out var value))
            {
                Throw();
            }
            return value;

            static void Throw() => throw new ObjectDisposedException("The cache item has been recycled before the value was obtained");
        }
    }
}
