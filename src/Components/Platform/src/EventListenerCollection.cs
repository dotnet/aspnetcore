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
    private readonly JSCallbackFactory _callbackFactory;
    private readonly ILogger _logger;
    private readonly List<IAsyncDisposable> _listeners = [];
    private bool _disposed;

    public EventListenerCollection(JSCallbackFactory callbackFactory, ILogger logger)
    {
        _callbackFactory = callbackFactory;
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
        Func<TEvent, Task> listener,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        var eventListener = new EventListener<TEvent>(this, target, type, listener, cancellationToken, _logger);
        _listeners.Add(eventListener);

        try
        {
            await eventListener.AddAsync(_callbackFactory, shapesJson);
        }
        catch (Exception ex)
        {
            // The exception of the failed addition is the useful one.
            await TryDisposeAsync(eventListener);

            // Decided by the token, not by the error, as for other canceled browser operations.
            if (cancellationToken.IsCancellationRequested && ex is not OperationCanceledException)
            {
                throw new OperationCanceledException(ex.Message, ex, cancellationToken);
            }

            throw;
        }

        // The token can be canceled while addEventListener is on its way.
        if (cancellationToken.IsCancellationRequested)
        {
            await TryDisposeAsync(eventListener);
            cancellationToken.ThrowIfCancellationRequested();
        }

        eventListener.RemoveWhenCanceled();
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
            await TryDisposeAsync(listener);
        }
    }

    internal async ValueTask TryDisposeAsync(IAsyncDisposable listener)
    {
        try
        {
            await listener.DisposeAsync();
        }
        catch (Exception ex)
        {
            Log.ListenerRemovalFailed(_logger, ex);
        }
    }

    internal static partial class Log
    {
        [LoggerMessage(2, LogLevel.Warning, "A listener could not be removed from its event target.", EventName = "ListenerRemovalFailed")]
        public static partial void ListenerRemovalFailed(ILogger logger, Exception exception);

        [LoggerMessage(1, LogLevel.Error, "A listener for the '{EventType}' event threw an exception.", EventName = "ListenerThrew")]
        public static partial void ListenerThrew(ILogger logger, string eventType, Exception exception);
    }
}
