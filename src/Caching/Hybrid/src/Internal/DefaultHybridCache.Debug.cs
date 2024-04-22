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

    internal int DebugGetOutstandingBuffers(bool flush = false)
                => flush ? Interlocked.Exchange(ref _outstandingBufferCount, 0) : Volatile.Read(ref _outstandingBufferCount);

    [Conditional("DEBUG")]
    internal void DebugDecrementOutstandingBuffers()
    {
        Interlocked.Decrement(ref _outstandingBufferCount);
    }

    [Conditional("DEBUG")]
    internal void DebugIncrementOutstandingBuffers()
    {
        Interlocked.Increment(ref _outstandingBufferCount);
    }
#endif

    partial class MutableCacheItem<T>
    {
        partial void DebugDecrementOutstandingBuffers();
        partial void DebugTrackBufferCore(DefaultHybridCache cache);

        [Conditional("DEBUG")]
        internal void DebugTrackBuffer(DefaultHybridCache cache) => DebugTrackBufferCore(cache);

#if DEBUG
        private DefaultHybridCache? _cache; // for buffer-tracking - only enabled in DEBUG
        partial void DebugDecrementOutstandingBuffers()
        {
            if (_buffer.ReturnToPool)
            {
                _cache?.DebugDecrementOutstandingBuffers();
            }
        }
        partial void DebugTrackBufferCore(DefaultHybridCache cache)
        {
            _cache = cache;
            if (_buffer.ReturnToPool)
            {
                _cache?.DebugIncrementOutstandingBuffers();
            }
        }
#endif
    }
}
