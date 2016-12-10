// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.ResponseCaching.Internal
{
    public class MemoryResponseCache : IResponseCache
    {
        private readonly IMemoryCache _cache;

        public MemoryResponseCache(IMemoryCache cache)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            _cache = cache;
        }

        public Task<IResponseCacheEntry> GetAsync(string key)
        {
            var entry = _cache.Get(key);

            var memoryCachedResponse = entry as MemoryCachedResponse;
            if (memoryCachedResponse != null)
            {
                return Task.FromResult<IResponseCacheEntry>(new CachedResponse
                {
                    Created = memoryCachedResponse.Created,
                    StatusCode = memoryCachedResponse.StatusCode,
                    Headers = memoryCachedResponse.Headers,
                    Body = new SegmentReadStream(memoryCachedResponse.BodySegments, memoryCachedResponse.BodyLength)
                });
            }
            else
            {
                return Task.FromResult(entry as IResponseCacheEntry);
            }
        }

        public async Task SetAsync(string key, IResponseCacheEntry entry, TimeSpan validFor)
        {
            var cachedResponse = entry as CachedResponse;
            if (cachedResponse != null)
            {
                var segmentStream = new SegmentWriteStream(StreamUtilities.BodySegmentSize);
                await cachedResponse.Body.CopyToAsync(segmentStream);

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
                        AbsoluteExpirationRelativeToNow = validFor
                    });
            }
            else
            {
                _cache.Set(
                    key,
                    entry,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = validFor
                    });
            }
        }
    }
}