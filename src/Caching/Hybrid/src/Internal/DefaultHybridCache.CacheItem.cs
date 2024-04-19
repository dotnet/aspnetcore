// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

partial class DefaultHybridCache
{
    internal abstract class CacheItem
    {
        internal static readonly PostEvictionDelegate _sharedOnEviction = static (key, value, reason, state) =>
        {
            if (value is CacheItem item)
            {
                // perform per-item clean-up; this could be buffer recycling (if defensive copies needed),
                // or could be disposal
                item.OnEviction();
            }
        };

        public virtual void Release() { } // for recycling purposes

        public abstract bool NeedsEvictionCallback { get; } // do we need to call Release when evicted?

        public virtual void OnEviction() { } // only invoked if NeedsEvictionCallback reported true

        public abstract bool TryReserveBuffer(out BufferChunk buffer);

        public abstract bool DebugIsImmutable { get; }
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
