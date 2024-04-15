// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

partial class DefaultHybridCache
{
    private readonly ConcurrentDictionary<StampedeKey, StampedeState> currentOperations = new();

    internal int DebugGetCallerCount(string key, HybridCacheEntryFlags? flags = null)
    {
        var stampedeKey = new StampedeKey(key, flags ?? defaultFlags);
        return currentOperations.TryGetValue(stampedeKey, out var state) ? state.DebugCallerCount : 0;
    }

    // returns true for a new session (in which case: we need to start the work), false for a pre-existing session
    public bool GetOrCreateStampede<TState, T>(string key, HybridCacheEntryFlags flags, out StampedeState<TState, T> stampedeState, bool canBeCanceled)
    {
        var stampedeKey = new StampedeKey(key, flags);
        if (currentOperations.TryGetValue(stampedeKey, out var found))
        {
            var tmp = found as StampedeState<TState, T>;
            if (tmp is null)
            {
                ThrowWrongType(key);
            }

            if (tmp.TryAddCaller())
            {
                // we joined an existing session
                stampedeState = tmp;
                return false;
            }
        }

        // create a new session
        stampedeState = new StampedeState<TState, T>(this, stampedeKey, canBeCanceled);
        currentOperations[stampedeKey] = stampedeState;
        return true;

        [DoesNotReturn]
        static void ThrowWrongType(string key) => throw new InvalidOperationException($"All calls to {nameof(HybridCache)} with the same key should use the same data type")
        {
            Data = { { "CacheKey", key } }
        };
    }
}
