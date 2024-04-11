// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

/// <summary>
/// The inbuilt ASP.NET implementation of <see cref="HybridCache"/>
/// </summary>
internal sealed class DefaultHybridCache : HybridCache
{
    private readonly IDistributedCache backendCache;
    private readonly IServiceProvider services;
    private readonly HybridCacheOptions options;
    public DefaultHybridCache(IOptions<HybridCacheOptions> options, IDistributedCache backendCache, IServiceProvider services)
    {
        this.backendCache = backendCache ?? throw new ArgumentNullException(nameof(backendCache));
        this.services = services ?? throw new ArgumentNullException(nameof(services));
        this.options = options.Value;
    }
    internal HybridCacheOptions Options => options;

    public override ValueTask<T> GetOrCreateAsync<TState, T>(string key, TState state, Func<TState, CancellationToken, ValueTask<T>> underlyingDataCallback, HybridCacheEntryOptions? options = null, ICollection<string>? tags = null, CancellationToken token = default)
        => underlyingDataCallback(state, token); // pass-thru without caching for initial API pass

    public override ValueTask RemoveKeyAsync(string key, CancellationToken token = default)
        => default; // no cache, nothing to remove

    public override ValueTask RemoveTagAsync(string tag, CancellationToken token = default)
        => default; // no cache, nothing to remove

    public override ValueTask SetAsync<T>(string key, T value, HybridCacheEntryOptions? options = null, ICollection<string>? tags = null, CancellationToken token = default)
        => default; // no cache, nothing to set
}
