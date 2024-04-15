// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

partial class DefaultHybridCache
{
    private sealed class ImmutableCacheItem<T>(T value) : CacheItem<T>
    {
        public override T GetValue() => value;

        public override byte[]? TryGetBytes(out int length)
        {
            length = 0;
            return null;
        }
    }
}
