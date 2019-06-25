// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.AspNetCore.ResponseCaching
{
    internal class MemoryResponseCache : IResponseCache
    {
        private readonly IMemoryCache _cache;

        internal MemoryResponseCache(IMemoryCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public IResponseCacheEntry Get(string key)
        {
            var entry = _cache.Get(key);

            if (entry is MemoryCachedResponse memoryCachedResponse)
            {
                return new CachedResponse
                {
                    Created = memoryCachedResponse.Created,
                    StatusCode = memoryCachedResponse.StatusCode,
                    Headers = memoryCachedResponse.Headers,
                    Body = new SegmentReadStream(memoryCachedResponse.BodySegments, memoryCachedResponse.BodyLength)
                };
            }
            else
            {
                return entry as IResponseCacheEntry;
            }
        }

        public Task<IResponseCacheEntry> GetAsync(string key)
        {
            return Task.FromResult(Get(key));
        }

        public void Set(string key, IResponseCacheEntry entry, TimeSpan validFor)
        {
            if (entry is CachedResponse cachedResponse)
            {
                var segmentStream = new SegmentWriteStream(StreamUtilities.BodySegmentSize);
                cachedResponse.Body.CopyTo(segmentStream);

                _cache.Set(
                    key,
                    new MemoryCachedResponse
                    {
                        Created = cachedResponse.Created,
                        StatusCode = cachedResponse.StatusCode,
                        Headers = cachedResponse.Headers,
                        BodySegments = segmentStream.GetSegments(),
                        BodyLength = segmentStream.Length
                    },
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = validFor,
                        Size = CacheEntryHelpers.EstimateCachedResponseSize(cachedResponse)
                    });
            }
            else
            {
                _cache.Set(
                    key,
                    entry,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = validFor,
                        Size = CacheEntryHelpers.EstimateCachedVaryByRulesySize(entry as CachedVaryByRules)
                    });
            }
        }

        public Task SetAsync(string key, IResponseCacheEntry entry, TimeSpan validFor)
        {
            Set(key, entry, validFor);
            return Task.CompletedTask;
        }
    }
}
