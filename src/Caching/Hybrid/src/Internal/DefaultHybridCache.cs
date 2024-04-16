// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

/// <summary>
/// The inbuilt ASP.NET implementation of <see cref="HybridCache"/>.
/// </summary>
internal sealed partial class DefaultHybridCache : HybridCache
{
    private readonly IDistributedCache? backendCache;
    private readonly IMemoryCache localCache;
    private readonly IServiceProvider services; // we can't resolve per-type serializers until we see each T
    private readonly IHybridCacheSerializerFactory[] serializerFactories;
    private readonly HybridCacheOptions options;
    private readonly ILogger? logger;
    private readonly CacheFeatures features; // used to avoid constant type-testing

    private readonly HybridCacheEntryFlags hardFlags; // *always* present (for example, because no L2)
    private readonly HybridCacheEntryFlags defaultFlags; // note this already includes hardFlags
    private readonly TimeSpan defaultExpiration;
    private readonly TimeSpan defaultLocalCacheExpiration;

    private readonly DistributedCacheEntryOptions defaultDistributedCacheExpiration;

    [Flags]
    internal enum CacheFeatures
    {
        None = 0,
        BackendCache = 1 << 0,
        BackendBuffers = 1 << 1,
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private CacheFeatures GetFeatures(CacheFeatures mask) => features & mask;

    internal CacheFeatures GetFeatures() => features;

    // used to restrict features in test suite
    internal void DebugRemoveFeatures(CacheFeatures features) => Unsafe.AsRef(in this.features) &= ~features;

    public DefaultHybridCache(IOptions<HybridCacheOptions> options, IServiceProvider services)
    {
        this.services = services ?? throw new ArgumentNullException(nameof(services));
        this.localCache = services.GetRequiredService<IMemoryCache>();
        this.options = options.Value;
        this.logger = services.GetService<ILoggerFactory>()?.CreateLogger(typeof(HybridCache)); // note optional

        this.backendCache = services.GetService<IDistributedCache>(); // note optional

        // ignore L2 if it is really just the same L1, wrapped
        // (note not just an "is" test; if someone has a custom subclass, who knows what it does?)
        if (this.backendCache is not null
            && this.backendCache.GetType() == typeof(MemoryDistributedCache)
            && this.localCache.GetType() == typeof(MemoryCache))
        {
            this.backendCache = null;
        }

        // perform type-tests on the backend once only
        this.features |= backendCache switch
        {
            IBufferDistributedCache => CacheFeatures.BackendCache | CacheFeatures.BackendBuffers,
            not null => CacheFeatures.BackendCache,
            _ => CacheFeatures.None
        };

        // When resolving serializers via the factory API, we will want the *last* instance,
        // i.e. "last added wins"; we can optimize by reversing the array ahead of time, and
        // taking the first match
        var factories = services.GetServices<IHybridCacheSerializerFactory>().ToArray();
        Array.Reverse(factories);
        this.serializerFactories = factories;

        MaximumPayloadBytes = checked((int)this.options.MaximumPayloadBytes); // for now hard-limit to 2GiB

        var defaultEntryOptions = this.options.DefaultEntryOptions;

        if (this.backendCache is null)
        {
            this.hardFlags |= HybridCacheEntryFlags.DisableDistributedCache;
        }
        this.defaultFlags = (defaultEntryOptions?.Flags ?? HybridCacheEntryFlags.None) | this.hardFlags;
        this.defaultExpiration = defaultEntryOptions?.Expiration ?? TimeSpan.FromMinutes(5);
        this.defaultLocalCacheExpiration = defaultEntryOptions?.LocalCacheExpiration ?? TimeSpan.FromMinutes(1);
        this.defaultDistributedCacheExpiration = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = defaultExpiration };
    }

    internal IDistributedCache? BackendCache => backendCache;
    internal IMemoryCache LocalCache => localCache;

    internal HybridCacheOptions Options => options;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private HybridCacheEntryFlags GetEffectiveFlags(HybridCacheEntryOptions? options)
        => (options?.Flags | hardFlags) ?? defaultFlags;

    public override ValueTask<T> GetOrCreateAsync<TState, T>(string key, TState state, Func<TState, CancellationToken, ValueTask<T>> underlyingDataCallback, HybridCacheEntryOptions? options = null, IReadOnlyCollection<string>? tags = null, CancellationToken token = default)
    {
        bool canBeCanceled = token.CanBeCanceled;
        if (canBeCanceled)
        {
            token.ThrowIfCancellationRequested();
        }

        var flags = GetEffectiveFlags(options);
        if ((flags & HybridCacheEntryFlags.DisableLocalCacheRead) == 0 && localCache.TryGetValue(key, out var untyped) && untyped is CacheItem<T> typed)
        {
            // short-circuit
            return new(typed.GetValue());
        }

        if (GetOrCreateStampede<TState, T>(key, flags, out var stampede, canBeCanceled))
        {
            // new query; we're responsible for making it happen
            if (canBeCanceled)
            {
                // *we* might cancel, but someone else might be depending on the result; start the
                // work independently, then we'll with join the outcome
                stampede.QueueUserWorkItem(in state, underlyingDataCallback, options);
            }
            else
            {
                // we're going to run to completion; no need to get complicated
                _ = stampede.ExecuteDirectAsync(in state, underlyingDataCallback, options); // this larger task includes L2 write etc
                return stampede.UnwrapAsync();
            }
        }

        return stampede.JoinAsync(token);
    }

    public override ValueTask RemoveKeyAsync(string key, CancellationToken token = default)
    {
        localCache.Remove(key);
        return backendCache is null ? default : new(backendCache.RemoveAsync(key, token));
    }

    public override ValueTask RemoveTagAsync(string tag, CancellationToken token = default)
        => default; // tags not yet implemented

    public override ValueTask SetAsync<T>(string key, T value, HybridCacheEntryOptions? options = null, IReadOnlyCollection<string>? tags = null, CancellationToken token = default)
    {
        // since we're forcing a write: disable L1+L2 read; we'll use a direct pass-thru of the value as the callback, to reuse all the code;
        // note also that stampede token is not shared with anyone else
        var flags = GetEffectiveFlags(options) | (HybridCacheEntryFlags.DisableLocalCacheRead | HybridCacheEntryFlags.DisableDistributedCacheRead);
        var state = new StampedeState<T, T>(this, new StampedeKey(key, flags), token);
        return new(state.ExecuteDirectAsync(value, static (state, _) => new(state), options)); // note this spans L2 write etc
    }
}
