// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    /// Asynchronously gets the value associated with the key if it exists, or generates a new entry using the provided key and a value from the given factory if the key is not found.
    /// </summary>
    /// <typeparam name="T">The type of the data being considered.</typeparam>
    /// <typeparam name="TState">The type of additional state required by <paramref name="factory"/>.</typeparam>
    /// <param name="key">The key of the entry to look for or create.</param>
    /// <param name="factory">Provides the underlying data service is the data is not available in the cache.</param>
    /// <param name="state">Additional state required for <paramref name="factory"/>.</param>
    /// <param name="options">Additional options for this cache entry.</param>
    /// <param name="tags">The tags to associate with this cache item.</param>
    /// <param name="token">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The data, either from cache or the underlying data service.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Delegate differences make this unambiguous")]
    public abstract ValueTask<T> GetOrCreateAsync<TState, T>(string key, TState state, Func<TState, CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null, IReadOnlyCollection<string>? tags = null, CancellationToken token = default);

    /// <summary>
    /// Asynchronously gets the value associated with the key if it exists, or generates a new entry using the provided key and a value from the given factory if the key is not found.
    /// </summary>
    /// <typeparam name="T">The type of the data being considered.</typeparam>
    /// <param name="key">The key of the entry to look for or create.</param>
    /// <param name="factory">Provides the underlying data service is the data is not available in the cache.</param>
    /// <param name="options">Additional options for this cache entry.</param>
    /// <param name="tags">The tags to associate with this cache item.</param>
    /// <param name="token">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The data, either from cache or the underlying data service.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Delegate differences make this unambiguous")]
    public ValueTask<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null, IReadOnlyCollection<string>? tags = null, CancellationToken token = default)
        => GetOrCreateAsync(key, factory, WrappedCallbackCache<T>.Instance, options, tags, token);

    private static class WrappedCallbackCache<T> // per-T memoized helper that allows GetOrCreateAsync<T> and GetOrCreateAsync<TState, T> to share an implementation
    {
        // for the simple usage scenario (no TState), pack the original callback as the "state", and use a wrapper function that just unrolls and invokes from the state
        public static readonly Func<Func<CancellationToken, ValueTask<T>>, CancellationToken, ValueTask<T>> Instance = static (callback, ct) => callback(ct);
    }

    /// <summary>
    /// Asynchronously sets or overwrites the value associated with the key.
    /// </summary>
    /// <typeparam name="T">The type of the data being considered.</typeparam>
    /// <param name="key">The key of the entry to create.</param>
    /// <param name="value">The value to assign for this cache entry.</param>
    /// <param name="options">Additional options for this cache entry.</param>
    /// <param name="tags">The tags to associate with this cache entry.</param>
    /// <param name="token">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    public abstract ValueTask SetAsync<T>(string key, T value, HybridCacheEntryOptions? options = null, IReadOnlyCollection<string>? tags = null, CancellationToken token = default);

    /// <summary>
    /// Asynchronously removes the value associated with the key if it exists.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Not ambiguous in context")]
    public abstract ValueTask RemoveAsync(string key, CancellationToken token = default);

    /// <summary>
    /// Asynchronously removes the value associated with the key if it exists.
    /// </summary>
    /// <remarks>Implementors should treat <c>null</c> as empty</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Not ambiguous in context")]
    public virtual ValueTask RemoveAsync(IEnumerable<string> keys, CancellationToken token = default)
    {
        return keys switch
        {
            // for consistency with GetOrCreate/Set: interpret null as "none"
            null or ICollection<string> { Count: 0 } => default,
            ICollection<string> { Count: 1 } => RemoveAsync(keys.Single(), token),
            _ => ForEachAsync(this, keys, token),
        };

        // default implementation is to call RemoveKeyAsync for each key in turn
        static async ValueTask ForEachAsync(HybridCache @this, IEnumerable<string> keys, CancellationToken token)
        {
            foreach (var key in keys)
            {
                await @this.RemoveAsync(key, token).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Asynchronously removes all values associated with the specified tags.
    /// </summary>
    /// <remarks>Implementors should treat <c>null</c> as empty</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Not ambiguous in context")]
    public virtual ValueTask RemoveByTagAsync(IEnumerable<string> tags, CancellationToken token = default)
    {
        return tags switch
        {
            // for consistency with GetOrCreate/Set: interpret null as "none"
            null or ICollection<string> { Count: 0 } => default,
            ICollection<string> { Count: 1 } => RemoveByTagAsync(tags.Single(), token),
            _ => ForEachAsync(this, tags, token),
        };

        // default implementation is to call RemoveTagAsync for each key in turn
        static async ValueTask ForEachAsync(HybridCache @this, IEnumerable<string> keys, CancellationToken token)
        {
            foreach (var key in keys)
            {
                await @this.RemoveByTagAsync(key, token).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Asynchronously removes all values associated with the specified tag.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Not ambiguous in context")]
    public abstract ValueTask RemoveByTagAsync(string tag, CancellationToken token = default);
}
