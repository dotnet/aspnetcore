// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

partial class DefaultHybridCache
{
    private sealed class ImmutableCacheItem<T>(T value) : CacheItem<T> // used to hold types that do not require defensive copies
    {
        private static ImmutableCacheItem<T>? sharedDefault;
        public static ImmutableCacheItem<T> Default => sharedDefault ??= new(default!); // this is only used when the underlying store is disabled

        public override T GetValue() => value;

        public override bool TryGetBytes(out int length, [NotNullWhen(true)] out byte[]? data)
        {
            length = 0;
            data = null;
            return false;
        }
    }
}
