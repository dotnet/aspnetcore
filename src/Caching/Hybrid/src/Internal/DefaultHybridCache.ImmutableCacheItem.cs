// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

partial class DefaultHybridCache
{
    private sealed class ImmutableCacheItem<T>(T value) : CacheItem<T> // used to hold types that do not require defensive copies
    {
        private static ImmutableCacheItem<T>? SharedDefault;

        // this is only used when the underlying store is disabled; we don't need 100% singleton; "good enough is"
        public static ImmutableCacheItem<T> Default => SharedDefault ??= new(default!);

        public override T GetValue() => value;

        public override bool TryGetBytes(out int length, [NotNullWhen(true)] out byte[]? data)
        {
            length = 0;
            data = null;
            return false;
        }
    }
}
