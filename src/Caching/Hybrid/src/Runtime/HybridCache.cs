using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.Extensions.Caching.Hybrid;

/// <summary>
/// Provides multi-tier caching services building on <see cref="IDistributedCache"/> backends.
/// </summary>
public abstract class HybridCache
{
    /// <summary>
    /// Get data from the cache, or the underlying data service if not available.
    /// </summary>
    /// <typeparam name="T">The type of the data being considered</typeparam>
    /// <typeparam name="TState">The type of additional state required by <paramref name="underlyingDataCallback"/></typeparam>
    /// <param name="key">The unique key for this cache entry</param>
    /// <param name="underlyingDataCallback">Provides the underlying data service is the data is not available in the cache</param>
    /// <param name="state">Additional state required for <paramref name="underlyingDataCallback"/></param>
    /// <param name="options">Additional options for this cache entry</param>
    /// <param name="tags">The tags to associate with this cache item</param>
    /// <param name="token">Cancellation for this operation</param>
    /// <returns>The data, either from cache or the underlying data service</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Delegate differences make this unambiguous")]
    public abstract ValueTask<T> GetOrCreateAsync<TState, T>(string key, TState state, Func<TState, CancellationToken, ValueTask<T>> underlyingDataCallback,
        HybridCacheEntryOptions? options = null, IReadOnlyCollection<string>? tags = null, CancellationToken token = default);

    /// <summary>
    /// Get data from the cache, or the underlying data service if not available.
    /// </summary>
    /// <typeparam name="T">The type of the data being considered</typeparam>
    /// <param name="key">The unique key for this cache entry</param>
    /// <param name="underlyingDataCallback">Provides the underlying data service is the data is not available in the cache</param>
    /// <param name="options">Additional options for this cache entry</param>
    /// <param name="tags">The tags to associate with this cache item</param>
    /// <param name="token">Cancellation for this operation</param>
    /// <returns>The data, either from cache or the underlying data service</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Delegate differences make this unambiguous")]
    public ValueTask<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T>> underlyingDataCallback,
        HybridCacheEntryOptions? options = null, IReadOnlyCollection<string>? tags = null, CancellationToken token = default)
        => GetOrCreateAsync(key, underlyingDataCallback, WrappedCallbackCache<T>.Instance, options, tags, token);

    private static class WrappedCallbackCache<T> // per-T memoized helper that allows GetOrCreateAsync<T> and GetOrCreateAsync<TState, T> to share an implementation
    {
        // for the simple usage scenario (no TState), pack the original callback as the "state", and use a wrapper function that just unrolls and invokes from the state
        public static readonly Func<Func<CancellationToken, ValueTask<T>>, CancellationToken, ValueTask<T>> Instance = static (callback, ct) => callback(ct);
    }

    /// <summary>
    /// Manually insert or overwrite a cache entry.
    /// </summary>
    /// <typeparam name="T">The type of the data being considered</typeparam>
    /// <param name="key">The unique key for this cache entry</param>
    /// <param name="value">The value to assign for this cache item</param>
    /// <param name="options">Additional options for this cache entry</param>
    /// <param name="tags">The tags to associate with this cache item</param>
    /// <param name="token">Cancellation for this operation</param>
    public abstract ValueTask SetAsync<T>(string key, T value, HybridCacheEntryOptions? options = null, IReadOnlyCollection<string>? tags = null, CancellationToken token = default);

    /// <summary>
    /// Removes cache data with the specified key
    /// </summary>
    public abstract ValueTask RemoveKeyAsync(string key, CancellationToken token = default);

    /// <summary>
    /// Removes cache data with the specified keys
    /// </summary>
    public virtual ValueTask RemoveKeysAsync(IEnumerable<string> keys, CancellationToken token = default)
    {
        return keys switch
        {
            // for consistency with GetOrCreate/Set: interpret null as "none"
            null or ICollection<string> { Count: 0 } => default,
            ICollection<string> { Count: 1 } => RemoveTagAsync(keys.Single(), token),
            _ => Walk(this, keys, token),
        };

        // default implementation is to call RemoveKeyAsync for each key in turn
        static async ValueTask Walk(HybridCache @this, IEnumerable<string> keys, CancellationToken token)
        {
            foreach (var key in keys)
            {
                await @this.RemoveKeyAsync(key, token).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Removes cache data associated with the specified tags
    /// </summary>
    public virtual ValueTask RemoveTagsAsync(IEnumerable<string> tags, CancellationToken token = default)
    {
        return tags switch
        {
            // for consistency with GetOrCreate/Set: interpret null as "none"
            null or ICollection<string> { Count: 0 } => default,
            ICollection<string> { Count: 1 } => RemoveTagAsync(tags.Single(), token),
            _ => Walk(this, tags, token),
        };

        // default implementation is to call RemoveTagAsync for each key in turn
        static async ValueTask Walk(HybridCache @this, IEnumerable<string> keys, CancellationToken token)
        {
            foreach (var key in keys)
            {
                await @this.RemoveTagAsync(key, token).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Removes cache data associated with the specified tag
    /// </summary>
    public abstract ValueTask RemoveTagAsync(string tag, CancellationToken token = default);
}
