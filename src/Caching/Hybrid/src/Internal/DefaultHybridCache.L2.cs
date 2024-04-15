using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

partial class DefaultHybridCache
{
    internal async Task<ArraySegment<byte>> GetFromL2Async(string key, CancellationToken token)
    {
        if ((features & BackendFeatures.Buffers) == 0)
        {
            var bytes = await backendCache.GetAsync(key, token).ConfigureAwait(false);
            if (bytes is not null)
            {
                if (bytes.Length > MaximumPayloadBytes)
                {
                    ThrowQuota();
                }
                return new(bytes);
            }
        }
        else
        {
            using var writer = new RecyclableArrayBufferWriter<byte>(MaximumPayloadBytes);
            var cache = Unsafe.As<IBufferDistributedCache>(backendCache); // type-checked already
            if (await cache.TryGetAsync(key, writer, token).ConfigureAwait(false))
            {
                return new(writer.DetachCommitted(out var length), 0, length);
            }
        }
        return default;

        static void ThrowQuota() => throw new InvalidOperationException("Maximum cache length exceeded");
    }

    internal ValueTask SetL2Async(string key, byte[] value, int length, HybridCacheEntryOptions? options, CancellationToken token)
    {
        Debug.Assert(value.Length >= length);
        if ((features & BackendFeatures.Buffers) == 0)
        {
            if (value.Length > length)
            {
                Array.Resize(ref value, length);
            }
            return new(backendCache.SetAsync(key, value, GetOptions(options), token));
        }
        else
        {
            var cache = Unsafe.As<IBufferDistributedCache>(backendCache); // type-checked already
            return cache.SetAsync(key, new(value, 0, length), GetOptions(options), token);
        }
    }

    private DistributedCacheEntryOptions GetOptions(HybridCacheEntryOptions? options)
    {
        DistributedCacheEntryOptions? result = null;
        if (options is not null && options.Expiration.HasValue && options.Expiration.GetValueOrDefault() != defaultExpiration)
        {
            result = options.ToDistributedCacheEntryOptions();
        }
        return result ?? defaultDistributedCacheExpiration;
    }

    internal void SetL1<T>(string key, CacheItem<T> value, HybridCacheEntryOptions? options)
        => localCache.Set(key, value, options?.LocalCacheExpiration ?? defaultLocalCacheExpiration);
}
