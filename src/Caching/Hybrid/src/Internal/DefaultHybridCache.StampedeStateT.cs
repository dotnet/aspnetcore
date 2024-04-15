// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

partial class DefaultHybridCache
{
    internal sealed class StampedeState<TState, T> : StampedeState
    {
        private readonly TaskCompletionSource<T> result = new();
        private TState? state;
        private Func<TState, CancellationToken, ValueTask<T>>? underlying;

        public StampedeState(ConcurrentDictionary<StampedeKey, StampedeState> currentOperations, in StampedeKey key, bool canBeCanceled) : base(currentOperations, key, canBeCanceled) { }

        public void QueueUserWorkItem(in TState state, Func<TState, CancellationToken, ValueTask<T>> underlying)
        {
            Debug.Assert(this.underlying is null);
            Debug.Assert(underlying is not null);

            // initialize the callback state
            this.state = state;
            this.underlying = underlying;

#if NETCOREAPP3_0_OR_GREATER
            ThreadPool.UnsafeQueueUserWorkItem(this, false);
#else
            ThreadPool.UnsafeQueueUserWorkItem(SharedWaitCallback, this);
#endif
        }

        public Task<T> ExecuteDirectAsync(in TState state, Func<TState, CancellationToken, ValueTask<T>> underlying)
        {
            Debug.Assert(this.underlying is null);
            Debug.Assert(underlying is not null);

            // initialize the callback state
            this.state = state;
            this.underlying = underlying;

            Execute();
            return Task;
        }

        public override void Execute()
        {
            try
            {
                var pending = underlying!(state!, SharedToken);
                if (pending.IsCompleted)
                {
                    var underlyingResult = pending.GetAwaiter().GetResult();
                    RemoveCurrentOperation();
                    result.TrySetResult(underlyingResult);
                }
                else
                {
                    _ = Awaited(this, pending);
                }
            }
            catch (Exception ex)
            {
                RemoveCurrentOperation();
                result.TrySetException(ex);
            }

            static async Task Awaited(StampedeState<TState, T> @this, ValueTask<T> pending)
            {
                try
                {
                    var underlyingResult = await pending.ConfigureAwait(false);
                    @this.RemoveCurrentOperation();
                    @this.result.TrySetResult(underlyingResult);
                }
                catch (Exception ex)
                {
                    @this.RemoveCurrentOperation();
                    @this.result.TrySetException(ex);
                }
            }
        }

        public Task<T> Task => result.Task;

        protected override void SetCanceled() => result.TrySetCanceled(SharedToken);

        public ValueTask<T> JoinAsync(CancellationToken token)
        {
            // if the underlying has already completed, and/or our local token can't cancel: we
            // can simply wrap the shared task; otherwise, we need our own cancellation state
            return token.CanBeCanceled && !Task.IsCompleted ? WithCancellation(this, token) : new(Task);

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
                    return await stampede.Task.ConfigureAwait(false);
                }
                finally
                {
                    stampede.RemoveCaller();
                }
            }
        }
    }
}
