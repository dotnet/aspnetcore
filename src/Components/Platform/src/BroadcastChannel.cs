// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents a named browser broadcast channel.
/// </summary>
public sealed class BroadcastChannel : IAsyncDisposable
{
    private static EventType<BroadcastChannel, MessageEvent>? _messageEvent;
    private static EventType<BroadcastChannel, MessageEvent>? _messageErrorEvent;
    private readonly IJSRuntime _jsRuntime;
    private readonly FeatureContext _context;
    private IInternalAsyncDisposal? _listeners;
    private readonly IJSObjectReference _reference;
    private bool _disposed;

    internal BroadcastChannel(FeatureContext context, IJSObjectReference reference)
    {
        _jsRuntime = context.JSRuntime;
        _context = context;
        _reference = reference;
    }

    /// <summary>
    /// Gets the event that fires when another eligible channel posts a JSON message.
    /// </summary>
    public static EventType<BroadcastChannel, MessageEvent> MessageEvent
        => _messageEvent ??= EventTypes.Create<BroadcastChannel, MessageEventPayload, MessageEvent>("message", """[{"data":{"$json":true},"origin":null}]""", static (_, payload) => payload.ToMessageEvent());

    /// <summary>
    /// Gets the event that fires when the browser cannot deserialize a message.
    /// </summary>
    public static EventType<BroadcastChannel, MessageEvent> MessageErrorEvent
        => _messageErrorEvent ??= EventTypes.Create<BroadcastChannel, MessageEventPayload, MessageEvent>("messageerror", """[{"data":{"$json":true},"origin":null}]""", static (_, payload) => payload.ToMessageEvent());

    /// <summary>
    /// Adds an independent listener for the specified event. Listener exceptions are logged without removing the listener.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event object.</typeparam>
    /// <param name="type">The event to listen for.</param>
    /// <param name="listener">The function to call with each event.</param>
    /// <returns>An object that removes this registration when disposed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="listener"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">This channel has been disposed.</exception>
    /// <remarks>Disposing this channel also removes its listeners.</remarks>
    public ValueTask<IAsyncDisposable> AddEventListenerAsync<TEvent>(EventType<BroadcastChannel, TEvent> type, Func<TEvent, Task> listener)
        => AddEventListenerAsync(type, listener, CancellationToken.None);

    /// <summary>
    /// Adds an independent listener for the specified event. Listener exceptions are logged without removing the listener.
    /// Canceling <paramref name="cancellationToken"/> removes this registration.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event object.</typeparam>
    /// <param name="type">The event to listen for.</param>
    /// <param name="listener">The function to call with each event.</param>
    /// <param name="cancellationToken">The token that removes the listener when canceled.</param>
    /// <returns>An object that removes this registration when disposed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="listener"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">This channel has been disposed.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled before registration completed.</exception>
    /// <remarks>Removal prevents new callbacks but does not wait for running callbacks.</remarks>
    public async ValueTask<IAsyncDisposable> AddEventListenerAsync<TEvent>(EventType<BroadcastChannel, TEvent> type, Func<TEvent, Task> listener, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(listener);
        cancellationToken.ThrowIfCancellationRequested();

        var listeners = (EventListenerCollection)(_listeners ??= _context.CreateEventListenerCollection());

        return await type.AddListenerAsync(listeners, _reference, this, listener, cancellationToken);
    }

    /// <summary>
    /// Gets the channel name.
    /// </summary>
    /// <returns>The channel name.</returns>
    public ValueTask<string> GetNameAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _reference.GetValueAsync<string>("name");
    }

    /// <summary>
    /// Posts a literal string to other eligible broadcast channels with the same name.
    /// </summary>
    /// <param name="message">The string to post.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of posting, not delivery.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is <see langword="null"/>.</exception>
    public ValueTask PostMessageAsync(string message)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(message);

        return _reference.InvokeVoidAsync("postMessage", message);
    }

    /// <summary>
    /// Posts the JSON representation of a value to other eligible broadcast channels with the same name.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="message">The value to serialize and post.</param>
    /// <param name="jsonTypeInfo">The JSON serialization metadata for the value.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of posting, not delivery.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="jsonTypeInfo"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The supplied contract determines the JSON representation. CLR type identity and non-JSON structured-clone values are not preserved.
    /// JSON numbers are converted to JavaScript numbers, which can lose large-integer or decimal precision.
    /// </remarks>
    public ValueTask PostMessageAsync<T>(T message, JsonTypeInfo<T> jsonTypeInfo)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(jsonTypeInfo);

        return PostMessageAsync(JsonSerializer.SerializeToElement(message, jsonTypeInfo));
    }

    /// <summary>
    /// Posts a JSON-compatible value to other eligible broadcast channels with the same name.
    /// </summary>
    /// <param name="message">The JSON value to post.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of posting, not delivery.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="message"/> has an undefined value kind.</exception>
    /// <exception cref="ObjectDisposedException">This channel or the document containing <paramref name="message"/> has been disposed.</exception>
    /// <remarks>Only JSON values are supported. Transferable objects and other structured-clone values are not supported.</remarks>
    public ValueTask PostMessageAsync(JsonElement message)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return BrowserMessaging.PostMessageAsync(_jsRuntime, _reference, message);
    }

    /// <summary>
    /// Closes this channel.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    public ValueTask CloseAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _reference.InvokeVoidAsync("close");
    }

    /// <summary>
    /// Closes this channel and releases its resources.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_listeners is { } listeners)
        {
            await listeners.InternalDisposeAsync();
        }

        try
        {
            await _reference.InvokeVoidAsync("close").ConfigureAwait(false);
        }
        finally
        {
            await _reference.DisposeAsync().ConfigureAwait(false);
        }
    }
}
