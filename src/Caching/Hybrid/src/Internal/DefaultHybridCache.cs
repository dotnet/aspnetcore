// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;
internal sealed class DefaultHybridCache : HybridCache
{
    public override ValueTask<T> GetOrCreateAsync<TState, T>(string key, TState state, Func<TState, CancellationToken, ValueTask<T>> underlyingDataCallback, HybridCacheEntryOptions? options = null, ICollection<string>? tags = null, CancellationToken token = default)
        => underlyingDataCallback(state, token); // pass-thru without caching for initial API pass

    public override ValueTask RemoveKeyAsync(string key, CancellationToken token = default)
        => default; // no cache, nothing to remove

    public override ValueTask RemoveTagAsync(string tag, CancellationToken token = default)
        => default; // no cache, nothing to remove

    public override ValueTask SetAsync<T>(string key, T value, HybridCacheEntryOptions? options = null, ICollection<string>? tags = null, CancellationToken token = default)
        => default; // no cache, nothing to set
}
