// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

partial class DefaultHybridCache
{
    internal sealed class StampedeState<TState, T> : StampedeState
    {
        private readonly TaskCompletionSource<CacheItem<T>>? _result;
        private TState? _state;
        private Func<TState, CancellationToken, ValueTask<T>>? _underlying;

        private HybridCacheEntryOptions? _options;

        public StampedeState(DefaultHybridCache cache, in StampedeKey key, bool canBeCanceled)
            : base(cache, key, canBeCanceled)
        {
            _result = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public override Type Type => typeof(T);

        public StampedeState(DefaultHybridCache cache, in StampedeKey key, CancellationToken token)
            : base(cache, key, token) { } // no TCS in this case - this is for SetValue only

        public void QueueUserWorkItem(in TState state, Func<TState, CancellationToken, ValueTask<T>> underlying, HybridCacheEntryOptions? options)
        {
            Debug.Assert(_underlying is null);
            Debug.Assert(underlying is not null);

            // initialize the callback state
            _state = state;
            _underlying = underlying;
            _options = options;

#if NETCOREAPP3_0_OR_GREATER
            ThreadPool.UnsafeQueueUserWorkItem(this, false);
#else
            ThreadPool.UnsafeQueueUserWorkItem(SharedWaitCallback, this);
#endif
        }

        public Task ExecuteDirectAsync(in TState state, Func<TState, CancellationToken, ValueTask<T>> underlying, HybridCacheEntryOptions? options)
        {
            Debug.Assert(_underlying is null);
            Debug.Assert(underlying is not null);

            // initialize the callback state
            _state = state;
            _underlying = underlying;
            _options = options;

            return BackgroundFetchAsync();
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
                    var cacheItem = SetResult(await _underlying!(_state!, SharedToken).ConfigureAwait(false));

                    // note that at this point we've already released most or all of the waiting callers; everything
                    // else here is background

                    // write to L2 if appropriate
                    if ((Key.Flags & HybridCacheEntryFlags.DisableDistributedCacheWrite) == 0)
                    {
                        if (cacheItem.TryGetBytes(out int length, out var bytes))
                        {
                            // mutable; we've already serialized it for the shared cache item
                            await Cache.SetL2Async(Key.Key, bytes, length, _options, SharedToken).ConfigureAwait(false);
                        }
                        else
                        {
                            // immutable: we'll need to do the serialize ourselves
                            var writer = RecyclableArrayBufferWriter<byte>.Create(MaximumPayloadBytes); // note this lifetime spans the SetL2Async
                            Cache.GetSerializer<T>().Serialize(cacheItem.GetValue(), writer); // note GetValue() is fixed value here
                            bytes = writer.GetBuffer(out length);
                            await Cache.SetL2Async(Key.Key, bytes, length, _options, SharedToken).ConfigureAwait(false);
                            writer.Dispose(); // recycle on success
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

        public Task<CacheItem<T>> Task
        {
            get
            {
                Debug.Assert(_result is not null);
                return _result is null ? Invalid() : _result.Task;

                static Task<CacheItem<T>> Invalid() => System.Threading.Tasks.Task.FromException<CacheItem<T>>(new InvalidOperationException("Task should not be accessed for non-shared instances"));
            }
        }

        private void SetException(Exception ex)
        {
            if (_result is not null)
            {
                Cache.RemoveStampede(Key);
                _result.TrySetException(ex);
            }
        }

        private void SetResult(CacheItem<T> value)
        {
            if ((Key.Flags & HybridCacheEntryFlags.DisableLocalCacheWrite) == 0)
            {
                Cache.SetL1(Key.Key, value, _options); // we can do this without a TCS, for SetValue
            }

            if (_result is not null)
            {
                Cache.RemoveStampede(Key);
                _result.TrySetResult(value);
            }
        }

        private void SetDefaultResult()
        {
            // note we don't store this dummy result in L1 or L2
            if (_result is not null)
            {
                Cache.RemoveStampede(Key);
                _result.TrySetResult(ImmutableCacheItem<T>.Default);
            }
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

        public override void SetCanceled() => _result?.TrySetCanceled(SharedToken);

        private Task<T>? _sharedUnwrap;

        internal ValueTask<T> UnwrapAsync()
        {
            var task = Task;
#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            if (task.IsCompletedSuccessfully)
#else
            if (task.Status == TaskStatus.RanToCompletion)
#endif
            {
                return new(task.Result.GetValue());
            }

            // if the type is immutable, callers can share the final step too
            Task<T> result = ImmutableTypeCache<T>.IsImmutable ? (_sharedUnwrap ??= Awaited(Task)) : Awaited(Task);
            return new(result);

            static async Task<T> Awaited(Task<CacheItem<T>> task)
                => (await task.ConfigureAwait(false)).GetValue();
        }

        public ValueTask<T> JoinAsync(CancellationToken token)
        {
            // if the underlying has already completed, and/or our local token can't cancel: we
            // can simply wrap the shared task; otherwise, we need our own cancellation state
            return token.CanBeCanceled && !Task.IsCompleted ? WithCancellation(this, token) : UnwrapAsync();

            static async ValueTask<T> WithCancellation(StampedeState<TState, T> stampede, CancellationToken token)
            {
                var cancelStub = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
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
