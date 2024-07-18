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
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

/// <summary>
/// The inbuilt ASP.NET implementation of <see cref="HybridCache"/>.
/// </summary>
internal sealed partial class DefaultHybridCache : HybridCache
{
    private readonly IDistributedCache? _backendCache;
    private readonly IMemoryCache _localCache;
    private readonly IServiceProvider _services; // we can't resolve per-type serializers until we see each T
    private readonly IHybridCacheSerializerFactory[] _serializerFactories;
    private readonly HybridCacheOptions _options;
    private readonly ILogger _logger;
    private readonly CacheFeatures _features; // used to avoid constant type-testing

    private readonly HybridCacheEntryFlags _hardFlags; // *always* present (for example, because no L2)
    private readonly HybridCacheEntryFlags _defaultFlags; // note this already includes hardFlags
    private readonly TimeSpan _defaultExpiration;
    private readonly TimeSpan _defaultLocalCacheExpiration;

    private readonly DistributedCacheEntryOptions _defaultDistributedCacheExpiration;

    [Flags]
    internal enum CacheFeatures
    {
        None = 0,
        BackendCache = 1 << 0,
        BackendBuffers = 1 << 1,
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private CacheFeatures GetFeatures(CacheFeatures mask) => _features & mask;

    internal CacheFeatures GetFeatures() => _features;

    // used to restrict features in test suite
    internal void DebugRemoveFeatures(CacheFeatures features) => Unsafe.AsRef(in _features) &= ~features;

    public DefaultHybridCache(IOptions<HybridCacheOptions> options, IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _localCache = services.GetRequiredService<IMemoryCache>();
        _options = options.Value;
        _logger = services.GetService<ILoggerFactory>()?.CreateLogger(typeof(HybridCache)) ?? NullLogger.Instance;

        _backendCache = services.GetService<IDistributedCache>(); // note optional

        // ignore L2 if it is really just the same L1, wrapped
        // (note not just an "is" test; if someone has a custom subclass, who knows what it does?)
        if (_backendCache is not null
            && _backendCache.GetType() == typeof(MemoryDistributedCache)
            && _localCache.GetType() == typeof(MemoryCache))
        {
            _backendCache = null;
        }

        // perform type-tests on the backend once only
        _features |= _backendCache switch
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
        _serializerFactories = factories;

        MaximumPayloadBytes = checked((int)_options.MaximumPayloadBytes); // for now hard-limit to 2GiB

        var defaultEntryOptions = _options.DefaultEntryOptions;

        if (_backendCache is null)
        {
            _hardFlags |= HybridCacheEntryFlags.DisableDistributedCache;
        }
        _defaultFlags = (defaultEntryOptions?.Flags ?? HybridCacheEntryFlags.None) | _hardFlags;
        _defaultExpiration = defaultEntryOptions?.Expiration ?? TimeSpan.FromMinutes(5);
        _defaultLocalCacheExpiration = defaultEntryOptions?.LocalCacheExpiration ?? TimeSpan.FromMinutes(1);
        _defaultDistributedCacheExpiration = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _defaultExpiration };
    }

    internal IDistributedCache? BackendCache => _backendCache;
    internal IMemoryCache LocalCache => _localCache;

    internal HybridCacheOptions Options => _options;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private HybridCacheEntryFlags GetEffectiveFlags(HybridCacheEntryOptions? options)
        => (options?.Flags | _hardFlags) ?? _defaultFlags;

    public override ValueTask<T> GetOrCreateAsync<TState, T>(string key, TState state, Func<TState, CancellationToken, ValueTask<T>> underlyingDataCallback, HybridCacheEntryOptions? options = null, IReadOnlyCollection<string>? tags = null, CancellationToken token = default)
    {
        var canBeCanceled = token.CanBeCanceled;
        if (canBeCanceled)
        {
            token.ThrowIfCancellationRequested();
        }

        var flags = GetEffectiveFlags(options);
        if ((flags & HybridCacheEntryFlags.DisableLocalCacheRead) == 0 && _localCache.TryGetValue(key, out var untyped)
            && untyped is CacheItem<T> typed && typed.TryGetValue(out var value))
        {
            // short-circuit
            return new(value);
        }

        if (GetOrCreateStampedeState<TState, T>(key, flags, out var stampede, canBeCanceled))
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
                return stampede.UnwrapReservedAsync();
            }
        }

        return stampede.JoinAsync(token);
    }

    public override ValueTask RemoveAsync(string key, CancellationToken token = default)
    {
        _localCache.Remove(key);
        return _backendCache is null ? default : new(_backendCache.RemoveAsync(key, token));
    }

    public override ValueTask RemoveByTagAsync(string tag, CancellationToken token = default)
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
