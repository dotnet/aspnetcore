// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

/// <summary>
/// The inbuilt ASP.NET implementation of <see cref="HybridCache"/>.
/// </summary>
internal sealed partial class DefaultHybridCache : HybridCache
{
    private readonly IDistributedCache backendCache;
    private readonly IServiceProvider services;
    private readonly IHybridCacheSerializerFactory[] serializerFactories;
    private readonly HybridCacheOptions options;
    private readonly BackendFeatures features;

    [Flags]
    private enum BackendFeatures
    {
        None = 0,
        Buffers = 1 << 0,
    }

    public DefaultHybridCache(IOptions<HybridCacheOptions> options, IDistributedCache backendCache, IServiceProvider services)
    {
        this.backendCache = backendCache ?? throw new ArgumentNullException(nameof(backendCache));
        this.services = services ?? throw new ArgumentNullException(nameof(services));
        this.options = options.Value;

        // perform type-tests on the backend once only
        if (backendCache is IBufferDistributedCache)
        {
            this.features |= BackendFeatures.Buffers;
        }

        // When resolving serializers via the factory API, we will want the *last* instance,
        // i.e. "last added wins"; we can optimize by reversing the array ahead of time, and
        // taking the first match
        var factories = services.GetServices<IHybridCacheSerializerFactory>().ToArray();
        Array.Reverse(factories);
        this.serializerFactories = factories;
    }
    internal HybridCacheOptions Options => options;

    private bool BackendBuffers => (features & BackendFeatures.Buffers) != 0;

    public override ValueTask<T> GetOrCreateAsync<TState, T>(string key, TState state, Func<TState, CancellationToken, ValueTask<T>> underlyingDataCallback, HybridCacheEntryOptions? options = null, IReadOnlyCollection<string>? tags = null, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        if (GetOrCreateStampede<T>(key, HybridCacheEntryFlags.None, out var stampede))
        {
            // new query; we're responsible for making it happen
            _ = Task.Run(async () =>
            {
                try
                {
                    var result = await underlyingDataCallback(state, stampede.SharedToken).ConfigureAwait(false);
                    currentOperations.TryRemove(stampede.Key, out _);
                    stampede.SetResult(result);
                }
                catch (Exception ex)
                {
                    stampede.SetException(ex);
                }
            }, stampede.SharedToken);

            return JoinAsync(stampede, token);
        }

        return JoinAsync(stampede, token);
    }

    public override ValueTask RemoveKeyAsync(string key, CancellationToken token = default)
        => new(backendCache.RemoveAsync(key, token));

    public override ValueTask RemoveTagAsync(string tag, CancellationToken token = default)
        => default; // no cache, nothing to remove

    public override ValueTask SetAsync<T>(string key, T value, HybridCacheEntryOptions? options = null, IReadOnlyCollection<string>? tags = null, CancellationToken token = default)
        => default; // no cache, nothing to set
}
