// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

partial class DefaultHybridCache
{
    internal bool DebugTryGetCacheItem(string key, [NotNullWhen(true)] out CacheItem? value)
    {
        if (_localCache.TryGetValue(key, out var untyped) && untyped is CacheItem typed)
        {
            value = typed;
            return true;
        }
        value = null;
        return false;
    }

#if DEBUG // enable ref-counted buffers

    private int _outstandingBufferCount;

    internal int DebugOnlyGetOutstandingBuffers(bool flush = false)
                => flush ? Interlocked.Exchange(ref _outstandingBufferCount, 0) : Volatile.Read(ref _outstandingBufferCount);

    [Conditional("DEBUG")]
    internal void DebugOnlyDecrementOutstandingBuffers()
    {
        Interlocked.Decrement(ref _outstandingBufferCount);
    }

    [Conditional("DEBUG")]
    internal void DebugOnlyIncrementOutstandingBuffers()
    {
        Interlocked.Increment(ref _outstandingBufferCount);
    }
#endif

    partial class MutableCacheItem<T>
    {
        partial void DebugOnlyDecrementOutstandingBuffers();
        partial void DebugOnlyTrackBufferCore(DefaultHybridCache cache);

        [Conditional("DEBUG")]
        internal void DebugOnlyTrackBuffer(DefaultHybridCache cache) => DebugOnlyTrackBufferCore(cache);

#if DEBUG
        private DefaultHybridCache? _cache; // for buffer-tracking - only enabled in DEBUG
        partial void DebugOnlyDecrementOutstandingBuffers()
        {
            if (_buffer.ReturnToPool)
            {
                _cache?.DebugOnlyDecrementOutstandingBuffers();
            }
        }
        partial void DebugOnlyTrackBufferCore(DefaultHybridCache cache)
        {
            _cache = cache;
            if (_buffer.ReturnToPool)
            {
                _cache?.DebugOnlyIncrementOutstandingBuffers();
            }
        }
#endif
    }
}
