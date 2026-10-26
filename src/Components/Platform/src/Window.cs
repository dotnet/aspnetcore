// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents the active browser window.
/// </summary>
public sealed class Window : IInternalAsyncDisposal
{
    private readonly IJSRuntime _jsRuntime;
    private readonly JSCallbackFactory _callbackFactory;
    private readonly ILogger _eventListenerLogger;
    private readonly EventListenerCollection _listeners;
    private readonly Dictionary<(Type FeatureType, string? Subkey), object> _features = [];
    private Task<IJSObjectReference>? _referenceTask;
    private bool _disposed;

    internal Window(IJSRuntime jsRuntime, ILoggerFactory loggerFactory)
    {
        _jsRuntime = jsRuntime;
        _callbackFactory = new JSCallbackFactory(jsRuntime);
        _eventListenerLogger = loggerFactory.CreateLogger<EventListenerCollection>();
        _listeners = CreateEventListenerCollection();
    }

    /// <summary>
    /// Adds a listener that is called each time the specified event fires on the window.
    /// Other listeners, including those added by JavaScript code of the page, are not affected.
    /// An exception thrown by the listener is logged and does not remove the listener.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event object.</typeparam>
    /// <param name="type">The event to listen for, such as <c>Window.StorageEvent</c>.</param>
    /// <param name="listener">The function to call with each event.</param>
    /// <returns>An object that removes the listener when disposed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="listener"/> is <see langword="null"/>.</exception>
    public async ValueTask<IAsyncDisposable> AddEventListenerAsync<TEvent>(EventType<Window, TEvent> type, Func<TEvent, Task> listener)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(listener);

        // Adding and removing listeners changes unlocked state, so these awaits keep the renderer's synchronization context.
        var window = await GetReferenceAsync();

        return await type.AddListenerAsync(_listeners, window, this, listener);
    }

    /// <summary>
    /// Creates the collection that a projection uses to add event listeners to its JavaScript object.
    /// </summary>
    internal EventListenerCollection CreateEventListenerCollection()
        => new(_callbackFactory, _eventListenerLogger);

    private Task<IJSObjectReference> GetReferenceAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_referenceTask is null || _referenceTask.IsFaulted || _referenceTask.IsCanceled)
        {
            _referenceTask = _jsRuntime.GetValueAsync<IJSObjectReference>("window").AsTask();
        }

        return _referenceTask;
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

    async ValueTask IInternalAsyncDisposal.InternalDisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Listeners need the window reference to remove themselves, so they go first.
        await ((IInternalAsyncDisposal)_listeners).InternalDisposeAsync();

        // Don't wait for a pending acquisition. It could hold up scope disposal until the JS interop timeout.
        if (_referenceTask is { IsCompletedSuccessfully: true } referenceTask)
        {
            await referenceTask.Result.DisposeAsync().ConfigureAwait(false);
        }

        var features = _features.Values.ToArray();
        _features.Clear();

        foreach (var feature in features)
        {
            if (feature is IInternalAsyncDisposal disposable)
            {
                await disposable.InternalDisposeAsync().ConfigureAwait(false);
            }
        }

        // Features remove their listeners first, so the module is no longer needed.
        await ((IInternalAsyncDisposal)_callbackFactory).InternalDisposeAsync();
    }
}
