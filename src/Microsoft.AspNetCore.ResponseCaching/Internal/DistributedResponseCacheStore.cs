// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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

        public object Get(string key)
        {
            try
            {
                return CacheEntrySerializer.Deserialize(_cache.Get(key));
            }
            catch
            {
                // TODO: Log error
                return null;
            }
        }

        public void Remove(string key)
        {
            try
            {
                _cache.Remove(key);
            }
            catch
            {
                // TODO: Log error
            }
        }

        public void Set(string key, object entry, TimeSpan validFor)
        {
            try
            {
                _cache.Set(
                    key,
                    CacheEntrySerializer.Serialize(entry),
                    new DistributedCacheEntryOptions()
                    {
                        AbsoluteExpirationRelativeToNow = validFor
                    });
            }
            catch
            {
                // TODO: Log error
            }
        }
    }
}