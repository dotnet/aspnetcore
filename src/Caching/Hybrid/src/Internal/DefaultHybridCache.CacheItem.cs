// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

partial class DefaultHybridCache
{
    internal abstract class CacheItem
    {
        private int _refCount = 1; // the number of pending operations against this cache item
        // note: the ref count is the number of callers anticipating this value at any given time; initially,
        // it is one for a simple "get the value" flow, but if another call joins with us, it'll be incremented;
        // if either cancels, it will get decremented, with the entire flow being cancelled if it ever becomes
        // zero
        // this counter also drives cache lifetime, with the cache itself incrementing the count by one; in the
        // case of mutable data, cache eviction may reduce this to zero (in cooperation with any concurrent readers,
        // who incr/decr around their fetch), allowing safe buffer recycling

        internal int RefCount => Volatile.Read(ref _refCount);

        internal static readonly PostEvictionDelegate _sharedOnEviction = static (key, value, reason, state) =>
        {
            if (value is CacheItem item)
            {
                item.Release();
            }
        };

        public virtual bool NeedsEvictionCallback => false; // do we need to call Release when evicted?

        protected virtual void OnFinalRelease() { } // any required release semantics

        public abstract bool TryReserveBuffer(out BufferChunk buffer);

        public abstract bool DebugIsImmutable { get; }

        public bool Release() // returns true ONLY for the final release step
        {
            var newCount = Interlocked.Decrement(ref _refCount);
            Debug.Assert(newCount >= 0, "over-release detected");
            if (newCount == 0)
            {
                // perform per-item clean-up, i.e. buffer recycling (if defensive copies needed)
                OnFinalRelease();
                return true;
            }
            return false;
        }

        public bool TryReserve()
        {
            // this is basically interlocked increment, but with a check against:
            // a) incrementing upwards from zero
            // b) overflowing *back* to zero
            var oldValue = Volatile.Read(ref _refCount);
            do
            {
                if (oldValue is 0 or -1)
                {
                    return false; // already burned, or about to roll around back to zero
                }

                var updated = Interlocked.CompareExchange(ref _refCount, oldValue + 1, oldValue);
                if (updated == oldValue)
                {
                    return true; // we exchanged
                }
                oldValue = updated; // we failed, but we have an updated state
            } while (true);
        }

    }

    internal abstract class CacheItem<T> : CacheItem
    {
        internal static CacheItem<T> Create() => ImmutableTypeCache<T>.IsImmutable ? new ImmutableCacheItem<T>() : new MutableCacheItem<T>();

        // attempt to get a value that was *not* previously reserved
        public abstract bool TryGetValue(out T value);

        // get a value that *was* reserved, countermanding our reservation in the process
        public T GetReservedValue()
        {
            if (!TryGetValue(out var value))
            {
                Throw();
            }
            Release();
            return value;

            static void Throw() => throw new ObjectDisposedException("The cache item has been recycled before the value was obtained");
        }
    }
}
