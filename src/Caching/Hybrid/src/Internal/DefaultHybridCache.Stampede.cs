// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

partial class DefaultHybridCache
{
    private readonly ConcurrentDictionary<StampedeKey, StampedeState> currentOperations = new();

    internal int DebugGetCallerCount(string key, HybridCacheEntryFlags flags = HybridCacheEntryFlags.None)
    {
        var stampedeKey = new StampedeKey(key, flags);
        return currentOperations.TryGetValue(stampedeKey, out var state) ? state.DebugCallerCount : 0;
    }

    // returns true for a new session (in which case: we need to start the work), false for a pre-existing session
    public bool GetOrCreateStampede<T>(string key, HybridCacheEntryFlags flags, out StampedeState<T> state)
    {
        var stampedeKey = new StampedeKey(key, flags);
        if (currentOperations.TryGetValue(stampedeKey, out var found))
        {
            var tmp = found as StampedeState<T>;
            if (tmp is null)
            {
                ThrowWrongType(key);
            }

            if (tmp.TryAddCaller())
            {
                // we joined an existing session
                state = tmp;
                return false;
            }
        }

        // create a new session
        state = new StampedeState<T>(stampedeKey);
        currentOperations[stampedeKey] = state;
        return true;
    }

    private static ValueTask<T> JoinAsync<T>(StampedeState<T> stampede, CancellationToken token)
    {
        return token.CanBeCanceled ? WithCancellation(stampede, token) : new(stampede.Task);

        static async ValueTask<T> WithCancellation(StampedeState<T> stampede, CancellationToken token)
        {
            var cancelStub = new TaskCompletionSource<bool>();
            using var reg = token.Register(static obj =>
            {
                ((TaskCompletionSource<bool>)obj!).TrySetResult(true);
            }, cancelStub);

            try
            {
                var first = await Task.WhenAny(stampede.Task, cancelStub.Task).ConfigureAwait(false);
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

    [DoesNotReturn]
    static void ThrowWrongType(string key) => throw new InvalidOperationException($"All calls to {nameof(HybridCache)} with the same key should use the same data type")
    {
        Data = { { "CacheKey", key } }
    };
}
