// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

partial class DefaultHybridCache
{
    private sealed class ImmutableCacheItem<T> : CacheItem<T> // used to hold types that do not require defensive copies
    {
        private T _value = default!; // deferred until SetValue

        public void SetValue(T value) => _value = value;

        private static ImmutableCacheItem<T>? _sharedDefault;

        // get a shared instance that passes as "reserved"; doesn't need to be 100% singleton,
        // but we don't want to break the reservation rules either; if we can't reserve: create new
        public static ImmutableCacheItem<T> GetReservedShared()
        {
            var obj = Volatile.Read(ref _sharedDefault);
            if (obj is null || !obj.TryReserve())
            {
                obj = new();
                obj.TryReserve(); // this is reliable on a new instance
                Volatile.Write(ref _sharedDefault, obj);
            }
            return obj;
        }

        public override bool TryGetValue(out T value)
        {
            value = _value;
            return true; // always available
        }

        public override bool TryReserveBuffer(out BufferChunk buffer)
        {
            buffer = default;
            return false; // we don't have one to reserve!
        }

        public override bool DebugIsImmutable => true;
    }
}
