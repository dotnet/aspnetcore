// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

partial class DefaultHybridCache
{
    private sealed class ImmutableCacheItem<T> : CacheItem<T> // used to hold types that do not require defensive copies
    {
        private T _value = default!; // deferred until SetValue

        public void SetValue(T value) => _value = value;

        private static ImmutableCacheItem<T>? _sharedDefault;

        // this is only used when the underlying store is disabled; we don't need 100% singleton; "good enough is"
        public static ImmutableCacheItem<T> Default => _sharedDefault ??= new();

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
