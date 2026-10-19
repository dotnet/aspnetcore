// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Tracks the listeners added to one event target, so that scope disposal can remove them all.
/// </summary>
internal sealed partial class EventListenerCollection : IInternalAsyncDisposal
{
    private readonly JSCallbackFactory _callbacks;
    private readonly ILogger _logger;
    private readonly List<IAsyncDisposable> _listeners = [];
    private bool _disposed;

    public EventListenerCollection(JSCallbackFactory callbacks, ILogger logger)
    {
        _callbacks = callbacks;
        _logger = logger;
    }

    /// <summary>
    /// Adds a listener for <paramref name="type"/> to <paramref name="target"/>.
    /// </summary>
    /// <returns>The handle that removes the listener when disposed.</returns>
    public async Task<IAsyncDisposable> AddAsync<[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] TEvent>(
        IJSObjectReference target,
        string type,
        string shapesJson,
        Func<TEvent, Task> listener)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var eventListener = new EventListener<TEvent>(this, target, type, listener, _logger);
        _listeners.Add(eventListener);

        try
        {
            await eventListener.AddAsync(_callbacks, shapesJson).ConfigureAwait(false);
        }
        catch
        {
            await eventListener.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        return eventListener;
    }

    internal void Remove(IAsyncDisposable listener) => _listeners.Remove(listener);

    async ValueTask IInternalAsyncDisposal.InternalDisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var listener in _listeners.ToArray())
        {
            await listener.DisposeAsync().ConfigureAwait(false);
        }
    }

    internal static partial class Log
    {
        [LoggerMessage(1, LogLevel.Error, "A listener for the '{EventType}' event threw an exception.", EventName = "ListenerThrew")]
        public static partial void ListenerThrew(ILogger logger, string eventType, Exception exception);
    }
}
