// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.AspNetCore.ResponseCaching.Internal
{
    public class MemoryResponseCacheStore : IResponseCacheStore
    {
        private readonly IMemoryCache _cache;

        public MemoryResponseCacheStore(IMemoryCache cache)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            _cache = cache;
        }

        public object Get(string key)
        {
            return _cache.Get(key);
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
        }

        public void Set(string key, object entry, TimeSpan validFor)
        {
            _cache.Set(
                key,
                entry,
                new MemoryCacheEntryOptions()
                {
                    AbsoluteExpirationRelativeToNow = validFor
                });
        }
    }
}