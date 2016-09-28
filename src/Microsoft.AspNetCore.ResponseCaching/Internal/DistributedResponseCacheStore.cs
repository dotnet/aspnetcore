// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.AspNetCore.ResponseCaching.Internal
{
    public class DistributedResponseCacheStore : IResponseCacheStore
    {
        private readonly IDistributedCache _cache;

        public DistributedResponseCacheStore(IDistributedCache cache)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            _cache = cache;
        }

        public async Task<IResponseCacheEntry> GetAsync(string key)
        {
            try
            {
                return ResponseCacheEntrySerializer.Deserialize(await _cache.GetAsync(key));
            }
            catch
            {
                return null;
            }
        }

        public async Task SetAsync(string key, IResponseCacheEntry entry, TimeSpan validFor)
        {
            try
            {
                await _cache.SetAsync(
                    key,
                    ResponseCacheEntrySerializer.Serialize(entry),
                    new DistributedCacheEntryOptions()
                    {
                        AbsoluteExpirationRelativeToNow = validFor
                    });
            }
            catch { }
        }
    }
}