// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.Extensions.Caching.Hybrid.Internal.DefaultHybridCache;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

partial class DefaultHybridCache
{
    internal sealed class StampedeState<TState, T> : StampedeState
    {
        private readonly TaskCompletionSource<CacheItem<T>> result = new();
        private TState? state;
        private Func<TState, CancellationToken, ValueTask<T>>? underlying;
        private HybridCacheEntryOptions? options;

        public StampedeState(DefaultHybridCache cache, in StampedeKey key, bool canBeCanceled)
            : base(cache, key, canBeCanceled) { }

        public void QueueUserWorkItem(in TState state, Func<TState, CancellationToken, ValueTask<T>> underlying, HybridCacheEntryOptions? options)
        {
            Debug.Assert(this.underlying is null);
            Debug.Assert(underlying is not null);

            // initialize the callback state
            this.state = state;
            this.underlying = underlying;
            this.options = options;

#if NETCOREAPP3_0_OR_GREATER
            ThreadPool.UnsafeQueueUserWorkItem(this, false);
#else
            ThreadPool.UnsafeQueueUserWorkItem(SharedWaitCallback, this);
#endif
        }

        public ValueTask<T> ExecuteDirectAsync(in TState state, Func<TState, CancellationToken, ValueTask<T>> underlying, HybridCacheEntryOptions? options)
        {
            Debug.Assert(this.underlying is null);
            Debug.Assert(underlying is not null);

            // initialize the callback state
            this.state = state;
            this.underlying = underlying;
            this.options = options;

            Execute();
            return UnwrapAsync(Task);
        }

        public override void Execute() => _ = BackgroundFetchAsync();

        private async Task BackgroundFetchAsync()
        {
            try
            {
                // read from L2 if appropriate
                if ((Key.Flags & HybridCacheEntryFlags.DisableDistributedCacheRead) == 0)
                {
                    var result = await Cache.GetFromL2Async(Key.Key, SharedToken).ConfigureAwait(false);

                    if (result.Array is not null)
                    {
                        SetResult(result);
                        return;
                    }
                }

                // nothing from L2; invoke the underlying data store
                if ((Key.Flags & HybridCacheEntryFlags.DisableUnderlyingData) == 0)
                {
                    var cacheItem = SetResult(await underlying!(state!, SharedToken).ConfigureAwait(false));

                    // note that at this point we've already released most or all of the waiting callers; everything
                    // else here is background

                    // write to L2 if appropriate
                    if ((Key.Flags & HybridCacheEntryFlags.DisableDistributedCacheWrite) == 0)
                    {
                        var bytes = cacheItem.TryGetBytes(out int length);
                        if (bytes is not null)
                        {
                            // we've already serialized it for the shared cache item
                            await Cache.SetL2Async(Key.Key, bytes, length, options, SharedToken).ConfigureAwait(false);
                        }
                        else
                        {
                            // we'll need to do the serialize ourselves
                            using var writer = new RecyclableArrayBufferWriter<byte>(MaximumPayloadBytes); // note this lifetime spans the SetL2Async
                            bytes = writer.GetBuffer(out length);
                            await Cache.SetL2Async(Key.Key, bytes, length, options, SharedToken).ConfigureAwait(false);
                        }
                    }
                }
                else
                {
                    // can't read from data store; implies we shouldn't write
                    // back to anywhere else, either
                    SetDefaultResult();
                }
            }
            catch (Exception ex)
            {
                SetException(ex);
            }
        }

        public Task<CacheItem<T>> Task => result.Task;

        private void SetException(Exception ex)
        {
            Cache.RemoveStampede(Key);
            result.TrySetException(ex);
        }

        private void SetResult(CacheItem<T> value)
        {
            if ((Key.Flags & HybridCacheEntryFlags.DisableLocalCacheWrite) == 0)
            {
                Cache.SetL1(Key.Key, value, options);
            }

            Cache.RemoveStampede(Key);
            result.TrySetResult(value);
        }

        private void SetDefaultResult()
        {
            // note we don't store this dummy result in L1 or L2
            Cache.RemoveStampede(Key);
            result.TrySetResult(ImmutableCacheItem<T>.Default);
        }

        private void SetResult(ArraySegment<byte> value)
        {
            // set a result from L2 cache
            Debug.Assert(value.Array is not null && value.Offset == 0);

            var serializer = Cache.GetSerializer<T>();
            CacheItem<T> cacheItem = ImmutableTypeCache<T>.IsImmutable
                ? new ImmutableCacheItem<T>(serializer.Deserialize(new(value.Array!, value.Offset, value.Count))) // deserialize
                : new MutableCacheItem<T>(value.Array!, value.Count, Cache.GetSerializer<T>()); // store the same bytes

            SetResult(cacheItem);
        }

        private CacheItem<T> SetResult(T value)
        {
            // set a result from a value we calculated directly
            CacheItem<T> cacheItem = ImmutableTypeCache<T>.IsImmutable
                ? new ImmutableCacheItem<T>(value) // no serialize needed
                : new MutableCacheItem<T>(value, Cache.GetSerializer<T>(), MaximumPayloadBytes); // serialization happens here

            SetResult(cacheItem);
            return cacheItem;
        }

        protected override void SetCanceled() => result.TrySetCanceled(SharedToken);

        public ValueTask<T> JoinAsync(CancellationToken token)
        {
            // if the underlying has already completed, and/or our local token can't cancel: we
            // can simply wrap the shared task; otherwise, we need our own cancellation state
            return token.CanBeCanceled && !Task.IsCompleted ? WithCancellation(this, token) : UnwrapAsync(Task);

            static async ValueTask<T> WithCancellation(StampedeState<TState, T> stampede, CancellationToken token)
            {
                var cancelStub = new TaskCompletionSource<bool>();
                using var reg = token.Register(static obj =>
                {
                    ((TaskCompletionSource<bool>)obj!).TrySetResult(true);
                }, cancelStub);

                try
                {
                    var first = await System.Threading.Tasks.Task.WhenAny(stampede.Task, cancelStub.Task).ConfigureAwait(false);
                    if (ReferenceEquals(first, cancelStub.Task))
                    {
                        // we expect this to throw, because otherwise we wouldn't have gotten here
                        token.ThrowIfCancellationRequested(); // get an appropriate exception
                    }
                    Debug.Assert(ReferenceEquals(first, stampede.Task));

                    // this has already completed, but we'll get the stack nicely
                    return (await stampede.Task.ConfigureAwait(false)).GetValue();
                }
                finally
                {
                    stampede.RemoveCaller();
                }
            }
        }
    }
}
