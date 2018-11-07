// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Caching.Memory
{
    public static class MemoryCacheWeakReferenceExtensions
    {
        public static TItem GetWeak<TItem>(this IMemoryCache cache, object key) where TItem : class
        {
            if (cache.TryGetValue<WeakReference<TItem>>(key, out WeakReference<TItem> reference))
            {
                reference.TryGetTarget(out TItem value);
                return value;
            }
            return null;
        }

        public static TItem SetWeak<TItem>(this IMemoryCache cache, object key, TItem value) where TItem : class
        {
            using (var entry = cache.CreateEntry(key))
            {
                var reference = new WeakReference<TItem>(value);
                entry.AddExpirationToken(new WeakToken<TItem>(reference));
                entry.Value = reference;
            }

            return value;
        }
    }
}
