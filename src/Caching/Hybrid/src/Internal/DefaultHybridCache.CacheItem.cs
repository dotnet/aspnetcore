// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

partial class DefaultHybridCache
{
    internal abstract class CacheItem<T>
    {
        public abstract T GetValue();

        public abstract bool TryGetBytes(out int length, [NotNullWhen(true)] out byte[]? data);
    }
}
