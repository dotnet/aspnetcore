// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

partial class DefaultHybridCache
{
    internal abstract class StampedeState
#if NETCOREAPP3_0_OR_GREATER
        : IThreadPoolWorkItem
#endif
    {
        private readonly DefaultHybridCache _cache;
        private readonly CacheItem _cacheItem;

        // because multiple callers can enlist, we need to track when the *last* caller cancels
        // (and keep going until then); that means we need to run with custom cancellation
        private readonly CancellationTokenSource? _sharedCancellation;
        internal readonly CancellationToken SharedToken; // this might have a value even when _sharedCancellation is null

        // we expose the key as a by-ref readonly; this minimizes the stack work involved in passing the key around
        // (both in terms of width and copy-semantics)
        private readonly StampedeKey _key;
        public ref readonly StampedeKey Key => ref _key;
        protected CacheItem CacheItem => _cacheItem;

        /// <summary>
        /// Create a stamped token optionally with shared cancellation support
        /// </summary>
        protected StampedeState(DefaultHybridCache cache, in StampedeKey key, CacheItem cacheItem, bool canBeCanceled)
        {
            _cache = cache;
            _key = key;
            _cacheItem = cacheItem;
            if (canBeCanceled)
            {
                // if the first (or any) caller can't be cancelled; we'll never get to zero; no point tracking
                // (in reality, all callers usually use the same path, so cancellation is usually "all" or "none")
                _sharedCancellation = new();
                SharedToken = _sharedCancellation.Token;
            }
            else
            {
                SharedToken = CancellationToken.None;
            }
        }

        /// <summary>
        /// Create a stamped token using a fixed cancellation token
        /// </summary>
        protected StampedeState(DefaultHybridCache cache, in StampedeKey key, CacheItem cacheItem, CancellationToken token)
        {
            _cache = cache;
            _key = key;
            _cacheItem = cacheItem;
            SharedToken = token;
        }

#if !NETCOREAPP3_0_OR_GREATER
        protected static readonly WaitCallback SharedWaitCallback = static obj => Unsafe.As<StampedeState>(obj).Execute();
#endif

        protected DefaultHybridCache Cache => _cache;

        public abstract void Execute();

        protected int MaximumPayloadBytes => _cache.MaximumPayloadBytes;

        public override string ToString() => Key.ToString();

        public abstract void SetCanceled();

        public int DebugCallerCount => _cacheItem.RefCount;

        public abstract Type Type { get; }

        public void CancelCaller()
        {
            // note that TryAddCaller has protections to avoid getting back from zero
            if (_cacheItem.Release())
            {
                // we're the last to leave; turn off the lights
                _sharedCancellation?.Cancel();
                SetCanceled();
            }
        }

        public bool TryAddCaller() => _cacheItem.TryReserve();
    }

    private void RemoveStampedeState(in StampedeKey key)
    {
        lock (GetPartitionedSyncLock(in key)) // see notes in SyncLock.cs
        {
            _currentOperations.TryRemove(key, out _);
        }
    }
}
