// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents the active browser window.
/// </summary>
public sealed class Window : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly Dictionary<(Type FeatureType, string? Subkey), object> _features = [];
    private bool _disposed;

    internal Window(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Gets the projection of type <typeparamref name="T"/>, creating it on first access.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="factory">The callback that creates the projection.</param>
    /// <returns>The projection keyed by <typeparamref name="T"/>.</returns>
    internal T GetOrAddFeature<T>(Func<IJSRuntime, T> factory) where T : class
        => GetOrAddFeatureCore(subkey: null, factory);

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
    internal T GetOrAddFeature<T>(string subkey, Func<IJSRuntime, T> factory) where T : class
    {
        ArgumentNullException.ThrowIfNull(subkey);

        return GetOrAddFeatureCore(subkey, factory);
    }

    private T GetOrAddFeatureCore<T>(string? subkey, Func<IJSRuntime, T> factory) where T : class
    {
        ArgumentNullException.ThrowIfNull(factory);

        ObjectDisposedException.ThrowIf(_disposed, this);

        var key = (typeof(T), subkey);

        if (_features.TryGetValue(key, out var existing))
        {
            return (T)existing;
        }

        var created = factory(_jsRuntime);
        _features[key] = created;

        return created;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
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
            if (feature is IAsyncDisposable disposable)
            {
                await disposable.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
