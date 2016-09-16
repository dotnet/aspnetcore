// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;

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

        public Task<object> GetAsync(string key)
        {
            return Task.FromResult(_cache.Get(key));
        }

        public Task RemoveAsync(string key)
        {
            _cache.Remove(key);
            return TaskCache.CompletedTask;
        }

        public Task SetAsync(string key, object entry, TimeSpan validFor)
        {
            _cache.Set(
                key,
                entry,
                new MemoryCacheEntryOptions()
                {
                    AbsoluteExpirationRelativeToNow = validFor
                });
            return TaskCache.CompletedTask;
        }
    }
}