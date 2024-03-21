// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;

namespace Microsoft.Extensions.Caching.Distributed;

// Stampede protection for hybrid cache
//
// The intent here is to share load, i.e. if 5 concurrent callers request the same cache item, they share
// the same async downstream call; however, this is complicated by cancellation: we can't just use the
// cancellation of the first caller, as we don't want the first request being cancelled to effectively
// cancel a bunch of unrelated requests. This means we need to create a cancellation proxy, and track
// clients as they become disconnected
internal partial class DefaultHybridCache
{
    private readonly ConcurrentDictionary<string, IStampedeToken> _inFlightOperations = new();

    private interface IStampedeToken
    {
        Type Type { get; }
        bool TryAddCaller(CancellationToken cancellationToken);
        void RemoveCaller();
    }
    private sealed class StampedeToken<T> : TaskCompletionSource<T>, IStampedeToken
    {
        public StampedeToken(CancellationToken cancellationToken) : base(TaskCreationOptions.RunContinuationsAsynchronously)
        {
            // if the *first* caller can't cancel: we don't need to track CTS at all
            // (strictly speaking, as soon as *any* caller can't cancel, we don't need to track CTS,
            // but in reality common callers are going to share a code-path, so it isn't worth
            // worrying too much about the "first caller could, second couldn't, third could" scenario
            // (we can revisit this later if we want)
            if (cancellationToken.CanBeCanceled)
            {
                _combinedCancellation = new();
                ObserveCancellation(cancellationToken);
            }
        }

        private readonly CancellationTokenSource? _combinedCancellation = new();

        Type IStampedeToken.Type => typeof(T);

        public async Task ExecuteAsync<TState>(TState state, Func<TState, CancellationToken, ValueTask<T>> callback)
        {
            try
            {
                var cancellation = _combinedCancellation is null ? CancellationToken.None : _combinedCancellation.Token;
                if (cancellation.IsCancellationRequested)
                {
                    TrySetCanceled(cancellation);
                }
                else
                {
                    TrySetResult(await callback(state, cancellation));
                }
            }
            catch (OperationCanceledException canceled)
            {
                TrySetCanceled(canceled.CancellationToken);
            }
            catch (Exception ex)
            {
                TrySetException(ex);
            }
        }

        private int _callerCount = 1;
        public bool TryAddCaller(CancellationToken cancellationToken)
        {
            // if we've decided that we don't need cancellation tracking,
            // we can avoid all of this
            if (_combinedCancellation is not null)
            {
                int oldCount;
                do
                {
                    oldCount = Volatile.Read(ref _callerCount);
                    if (oldCount == 0)
                    {
                        return false; // already dead, sorry
                    }
                } while (Interlocked.CompareExchange(ref _callerCount, checked(oldCount + 1), oldCount) != oldCount);

                ObserveCancellation(cancellationToken);
            }
            return true;
        }

        private void ObserveCancellation(CancellationToken cancellationToken)
        {
            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(static obj => ((IStampedeToken)obj!).RemoveCaller(), this);
            }
        }

        public void RemoveCaller() // one of the callers became cancelled; if that leaves zero interested: cancel the underlying data operation
        {
            if (_combinedCancellation is not null && Interlocked.Decrement(ref _callerCount) == 0)
            {
                // there is a possibility that the down-stream operation can fault in weird ways
                // when cancelled, and there's no-one left to observe it; add an observer ourselves
                // (this just prevents unobserved task exception reports)
                _ = Task.ContinueWith(static task =>
                {
                    _ = task.Exception;
                }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);

                _combinedCancellation.Cancel();
            }
        }
    }

    private Task<TValue> GetDownstreamAsync<TState, TValue>(string key, TState state, Func<TState, CancellationToken, ValueTask<TValue>> callback, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var token = GetOrCreateStampedeToken<TValue>(key, out var isNew, cancellationToken);

        if (isNew)
        {
            // we don't await the *actual operation*, because it might outlive us - if someone else joins in with
            // a longer cancellation - the *current* caller wants to exit based only on *their* cancellation, not the combined
            StartExecution(token, state, callback);
        }

        if (!cancellationToken.CanBeCanceled)
        {
            // nice and simple; just return the shared task
            return token.Task;
        }
        else
        {
            return AwaitSharedTaskWithLocalCancellation(token.Task, cancellationToken);
        }

        static async Task<TValue> AwaitSharedTaskWithLocalCancellation(Task<TValue> sharedResult, CancellationToken perCallCancellation)
        {
            perCallCancellation.ThrowIfCancellationRequested();
            Debug.Assert(perCallCancellation.CanBeCanceled);

            var cancelProxy = new TaskCompletionSource();
            using var reg = perCallCancellation.Register(static state => ((TaskCompletionSource)state!).SetResult(), cancelProxy);
            // wait for either shared completion or local cancellation; if the latter: propagate
            var first = await Task.WhenAny(sharedResult, cancelProxy.Task);
            if (ReferenceEquals(first, cancelProxy.Task))
            {
                Debug.Assert(perCallCancellation.IsCancellationRequested);
                perCallCancellation.ThrowIfCancellationRequested();
            }
            // otherwise, the shared task should be ready; propagate the result
            Debug.Assert(sharedResult.IsCompleted);
            return await sharedResult;
        }

        static void StartExecution(StampedeToken<TValue> token, TState state, Func<TState, CancellationToken, ValueTask<TValue>> callback)
        {
            // logically what we want here is as below, but we want to do it without the capture and delegate allocs;
            // note that ExecuteAsync is non-faulting (it reports faults back via the TCS)
            // _ = Task.Run(() => token.ExecuteAsync(state, callback), CancellationToken.None);

            ThreadPool.QueueUserWorkItem(static async tuple =>
            {
                await tuple.token.ExecuteAsync(tuple.state, tuple.callback);
            }, (token, state, callback), false);
        }
    }

    private StampedeToken<T> GetOrCreateStampedeToken<T>(string key, out bool isNew, CancellationToken cancellationToken)
    {
        if (_inFlightOperations.TryGetValue(key, out var existingToken))
        {
            if (existingToken is not StampedeToken<T> typed)
            {
                throw new InvalidOperationException($"Cache item mismatch; existing query is '{existingToken.Type.FullName}' vs new query '{typeof(T).FullName}'");
            }
            if (existingToken.TryAddCaller(cancellationToken))
            {
                isNew = false;
                return typed;
            }
        }
        // otherwise, upsert the value; this is *probably* an insert, but there
        // is a niche case where the existing value is an outgoing cancelled item
        // (i.e. TryAddCaller failed); in that scenario, we'll risk having two
        // concurrent down-stream operations
        var newToken = new StampedeToken<T>(cancellationToken);
        _inFlightOperations[key] = newToken;
        isNew = true;
        return newToken;
    }
}
