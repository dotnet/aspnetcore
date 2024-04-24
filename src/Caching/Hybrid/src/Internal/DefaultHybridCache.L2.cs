// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

partial class DefaultHybridCache
{
    internal ValueTask<BufferChunk> GetFromL2Async(string key, CancellationToken token)
    {
        switch (GetFeatures(CacheFeatures.BackendCache | CacheFeatures.BackendBuffers))
        {
            case CacheFeatures.BackendCache: // legacy byte[]-based
                var pendingLegacy = _backendCache!.GetAsync(key, token);
#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                if (!pendingLegacy.IsCompletedSuccessfully)
#else
                if (pendingLegacy.Status != TaskStatus.RanToCompletion)
#endif
                {
                    return new(AwaitedLegacy(pendingLegacy, this));
                }
                return new(GetValidPayloadSegment(pendingLegacy.Result)); // already complete
            case CacheFeatures.BackendCache | CacheFeatures.BackendBuffers: // IBufferWriter<byte>-based
                var writer = RecyclableArrayBufferWriter<byte>.Create(MaximumPayloadBytes);
                var cache = Unsafe.As<IBufferDistributedCache>(_backendCache!); // type-checked already
                var pendingBuffers = cache.TryGetAsync(key, writer, token);
                if (!pendingBuffers.IsCompletedSuccessfully)
                {
                    return new(AwaitedBuffers(pendingBuffers, writer));
                }
                BufferChunk result = pendingBuffers.GetAwaiter().GetResult()
                    ? new(writer.DetachCommitted(out var length), length, returnToPool: true)
                    : default;
                writer.Dispose(); // it is not accidental that this isn't "using"; avoid recycling if not 100% sure what happened
                return new(result);
        }
        return default;

        static async Task<BufferChunk> AwaitedLegacy(Task<byte[]?> pending, DefaultHybridCache @this)
        {
            var bytes = await pending.ConfigureAwait(false);
            return @this.GetValidPayloadSegment(bytes);
        }

        static async Task<BufferChunk> AwaitedBuffers(ValueTask<bool> pending, RecyclableArrayBufferWriter<byte> writer)
        {
            BufferChunk result = await pending.ConfigureAwait(false)
                    ? new(writer.DetachCommitted(out var length), length, returnToPool: true)
                    : default;
            writer.Dispose(); // it is not accidental that this isn't "using"; avoid recycling if not 100% sure what happened
            return result;
        }
    }

    private BufferChunk GetValidPayloadSegment(byte[]? payload)
    {
        if (payload is not null)
        {
            if (payload.Length > MaximumPayloadBytes)
            {
                ThrowPayloadLengthExceeded(payload.Length);
            }
            return new(payload);
        }
        return default;
    }

    [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
    private void ThrowPayloadLengthExceeded(int size) // splitting the exception bits out to a different method
    {
        // TODO: also log to logger (hence instance method)
        throw new InvalidOperationException($"Maximum cache length ({MaximumPayloadBytes} bytes) exceeded");
    }

    internal ValueTask SetL2Async(string key, in BufferChunk buffer, HybridCacheEntryOptions? options, CancellationToken token)
    {
        Debug.Assert(buffer.Array is not null);
        switch (GetFeatures(CacheFeatures.BackendCache | CacheFeatures.BackendBuffers))
        {
            case CacheFeatures.BackendCache: // legacy byte[]-based
                var arr = buffer.Array;
                if (arr.Length != buffer.Length)
                {
                    // we'll need a right-sized snapshot
                    arr = buffer.ToArray();
                }
                return new(_backendCache!.SetAsync(key, arr, GetOptions(options), token));
            case CacheFeatures.BackendCache | CacheFeatures.BackendBuffers: // ReadOnlySequence<byte>-based
                var cache = Unsafe.As<IBufferDistributedCache>(_backendCache!); // type-checked already
                return cache.SetAsync(key, buffer.AsSequence(), GetOptions(options), token);
        }
        return default;
    }

    private DistributedCacheEntryOptions GetOptions(HybridCacheEntryOptions? options)
    {
        DistributedCacheEntryOptions? result = null;
        if (options is not null && options.Expiration.HasValue && options.Expiration.GetValueOrDefault() != _defaultExpiration)
        {
            result = options.ToDistributedCacheEntryOptions();
        }
        return result ?? _defaultDistributedCacheExpiration;
    }

    internal void SetL1<T>(string key, CacheItem<T> value, HybridCacheEntryOptions? options)
    {
        if (value.TryReserve()) // incr ref-count for the the cache itself; this *may* be released via the NeedsEvictionCallback path
        {
            // based on CacheExtensions.Set<TItem>, but with post-eviction recycling
            using var cacheEntry = _localCache.CreateEntry(key);
            cacheEntry.AbsoluteExpirationRelativeToNow = options?.LocalCacheExpiration ?? _defaultLocalCacheExpiration;
            cacheEntry.Value = value;
            if (value.NeedsEvictionCallback)
            {
                cacheEntry.RegisterPostEvictionCallback(CacheItem._sharedOnEviction);
            }
        }
    }
}
