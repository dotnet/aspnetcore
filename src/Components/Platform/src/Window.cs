// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents the active browser window.
/// </summary>
public sealed class Window : IInternalAsyncDisposal
{
    private readonly FeatureContext _context;
    private readonly Dictionary<(Type FeatureType, string? Subkey), object> _features = [];
    private bool _disposed;

    internal Window(IJSRuntime jsRuntime)
    {
        _context = new FeatureContext(jsRuntime);
    }

    /// <summary>
    /// Gets the projection of type <typeparamref name="T"/>, creating it on first access.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="factory">The callback that creates the projection.</param>
    /// <returns>The projection keyed by <typeparamref name="T"/>.</returns>
    internal T GetOrAddFeature<T>(Func<FeatureContext, T> factory) where T : class
    {
        ArgumentNullException.ThrowIfNull(factory);

        return GetOrAddFeatureCore(subkey: null, static (context, factory) => factory(context), factory);
    }

    /// <summary>
    /// Gets the projection of type <typeparamref name="T"/>, creating it on first access with
    /// <paramref name="factoryArgument"/>.
    /// </summary>
    /// <remarks>
    /// Use this overload when the factory needs a value other than the <see cref="FeatureContext"/>, such as
    /// the owner of the projection, so that the factory can be a <see langword="static"/> lambda.
    /// </remarks>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <typeparam name="TArg">The type of the argument to pass to <paramref name="factory"/>.</typeparam>
    /// <param name="factory">The callback that creates the projection.</param>
    /// <param name="factoryArgument">The argument to pass to <paramref name="factory"/>.</param>
    /// <returns>The projection keyed by <typeparamref name="T"/>.</returns>
    internal T GetOrAddFeature<T, TArg>(Func<FeatureContext, TArg, T> factory, TArg factoryArgument) where T : class
    {
        ArgumentNullException.ThrowIfNull(factory);

        return GetOrAddFeatureCore(subkey: null, factory, factoryArgument);
    }

    /// <summary>
    /// Gets the projection of type <typeparamref name="T"/> identified by <paramref name="subkey"/>,
    /// creating it on first access.
    /// </summary>
    /// <remarks>
    /// Use this overload when multiple projections share the same <typeparamref name="T"/>, such as
    /// the local and session <see cref="Storage"/> instances.
    /// </remarks>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="subkey">The key that distinguishes this projection from others of the same type.</param>
    /// <param name="factory">The callback that creates the projection.</param>
    /// <returns>The projection keyed by <typeparamref name="T"/> and <paramref name="subkey"/>.</returns>
    internal T GetOrAddFeature<T>(string subkey, Func<FeatureContext, T> factory) where T : class
    {
        ArgumentNullException.ThrowIfNull(subkey);
        ArgumentNullException.ThrowIfNull(factory);

        return GetOrAddFeatureCore(subkey, static (context, factory) => factory(context), factory);
    }

    private T GetOrAddFeatureCore<T, TArg>(string? subkey, Func<FeatureContext, TArg, T> factory, TArg factoryArgument) where T : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var key = (typeof(T), subkey);

        if (_features.TryGetValue(key, out var existing))
        {
            return (T)existing;
        }

        var created = factory(_context, factoryArgument);
        _features[key] = created;

        return created;
    }

    async ValueTask IInternalAsyncDisposal.InternalDisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        var features = _features.Values.ToArray();
        _features.Clear();

        foreach (var feature in features)
        {
            if (feature is IInternalAsyncDisposal disposable)
            {
                await disposable.InternalDisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
