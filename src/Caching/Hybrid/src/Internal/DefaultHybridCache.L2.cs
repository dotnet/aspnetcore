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
    internal ValueTask<ArraySegment<byte>> GetFromL2Async(string key, CancellationToken token)
    {
        switch (GetFeatures(CacheFeatures.BackendCache | CacheFeatures.BackendBuffers))
        {
            case CacheFeatures.BackendCache: // legacy byte[]-based
                var pendingLegacy = backendCache!.GetAsync(key, token);
#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                if (!pendingLegacy.IsCompletedSuccessfully)
#else
                if (pendingLegacy.Status != TaskStatus.RanToCompletion)
#endif
                {
                    return new(AwaitedLegacy(pendingLegacy, MaximumPayloadBytes));
                }
                var bytes = pendingLegacy.Result; // already complete
                if (bytes is not null)
                {
                    if (bytes.Length > MaximumPayloadBytes)
                    {
                        ThrowQuota();
                    }
                    return new(new ArraySegment<byte>(bytes));
                }
                break;
            case CacheFeatures.BackendCache | CacheFeatures.BackendBuffers: // IBufferWriter<byte>-based
                var writer = RecyclableArrayBufferWriter<byte>.Create(MaximumPayloadBytes);
                var cache = Unsafe.As<IBufferDistributedCache>(backendCache!); // type-checked already
                var pendingBuffers = cache.TryGetAsync(key, writer, token);
                if (!pendingBuffers.IsCompletedSuccessfully)
                {
                    return new(AwaitedBuffers(pendingBuffers, writer));
                }
                ArraySegment<byte> result = pendingBuffers.GetAwaiter().GetResult()
                    ? new(writer.DetachCommitted(out var length), 0, length)
                    : default;
                writer.Dispose(); // it is not accidental that this isn't "using"; avoid recycling if not 100% sure what happened
                return new(result);
        }
        return default;

        static async Task<ArraySegment<byte>> AwaitedLegacy(Task<byte[]?> pending, int maximumPayloadBytes)
        {
            var bytes = await pending.ConfigureAwait(false);
            if (bytes is not null)
            {
                if (bytes.Length > maximumPayloadBytes)
                {
                    ThrowQuota();
                }
                return new(bytes);
            }
            return default;
        }

        static async Task<ArraySegment<byte>> AwaitedBuffers(ValueTask<bool> pending, RecyclableArrayBufferWriter<byte> writer)
        {
            ArraySegment<byte> result = await pending.ConfigureAwait(false)
                    ? new(writer.DetachCommitted(out var length), 0, length)
                    : default;
            writer.Dispose(); // it is not accidental that this isn't "using"; avoid recycling if not 100% sure what happened
            return result;
        }

        static void ThrowQuota() => throw new InvalidOperationException("Maximum cache length exceeded");
    }

    internal ValueTask SetL2Async(string key, byte[] value, int length, HybridCacheEntryOptions? options, CancellationToken token)
    {
        Debug.Assert(value.Length >= length);
        switch (GetFeatures(CacheFeatures.BackendCache | CacheFeatures.BackendBuffers))
        {
            case CacheFeatures.BackendCache: // legacy byte[]-based
                if (value.Length > length)
                {
                    Array.Resize(ref value, length);
                }
                Debug.Assert(value.Length == length);
                return new(backendCache!.SetAsync(key, value, GetOptions(options), token));
            case CacheFeatures.BackendCache | CacheFeatures.BackendBuffers: // ReadOnlySequence<byte>-based
                var cache = Unsafe.As<IBufferDistributedCache>(backendCache!); // type-checked already
                return cache.SetAsync(key, new(value, 0, length), GetOptions(options), token);
        }
        return default;
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
