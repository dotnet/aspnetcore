// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents the orientation of the screen that the browser window is shown on.
/// </summary>
public sealed class ScreenOrientation : IInternalAsyncDisposal
{
    private readonly IJSRuntime _jsRuntime;
    private readonly EventListenerCollection _listeners;
    private Task<IJSObjectReference>? _referenceTask;
    private bool _disposed;

    internal ScreenOrientation(IJSRuntime jsRuntime, EventListenerCollection listeners)
    {
        _jsRuntime = jsRuntime;
        _listeners = listeners;
    }

    /// <summary>
    /// Gets the event that fires when the orientation of the screen changes.
    /// </summary>
    public static EventType<ScreenOrientation, Event> ChangeEvent { get; }
        = EventTypes.Create<ScreenOrientation, Event>("change", "[{}]");

    /// <summary>
    /// Gets the current orientation of the screen.
    /// </summary>
    /// <returns>The orientation of the screen, such as <see cref="OrientationType.LandscapePrimary"/>.</returns>
    /// <exception cref="NotSupportedException">The browser does not provide the Screen Orientation API.</exception>
    public async ValueTask<OrientationType> GetTypeAsync()
    {
        var orientation = await GetReferenceAsync().ConfigureAwait(false);

        return await orientation.GetValueAsync<OrientationType>("type").ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the angle of the screen from its natural orientation, in degrees.
    /// </summary>
    /// <returns>The angle, in degrees, such as 0 or 90.</returns>
    /// <exception cref="NotSupportedException">The browser does not provide the Screen Orientation API.</exception>
    public async ValueTask<ushort> GetAngleAsync()
    {
        var orientation = await GetReferenceAsync().ConfigureAwait(false);

        return await orientation.GetValueAsync<ushort>("angle").ConfigureAwait(false);
    }

    /// <summary>
    /// Adds a listener that is called each time the specified event fires on the screen orientation.
    /// Other listeners, including those added by JavaScript code of the page, are not affected.
    /// An exception thrown by the listener is logged and does not remove the listener.
    /// Each call creates an independent registration, even for the same listener;
    /// disposing the returned object removes only that registration.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event object.</typeparam>
    /// <param name="type">The event to listen for, such as <see cref="ChangeEvent"/>.</param>
    /// <param name="listener">The function to call with each event.</param>
    /// <returns>An object that removes the listener when disposed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="listener"/> is <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException">The browser does not provide the Screen Orientation API.</exception>
    public ValueTask<IAsyncDisposable> AddEventListenerAsync<TEvent>(EventType<ScreenOrientation, TEvent> type, Func<TEvent, Task> listener)
        => AddEventListenerAsync(type, listener, CancellationToken.None);

    /// <summary>
    /// Adds a listener that is called each time the specified event fires on the screen orientation.
    /// Other listeners, including those added by JavaScript code of the page, are not affected.
    /// An exception thrown by the listener is logged and does not remove the listener.
    /// Each call creates an independent registration, even for the same listener;
    /// disposing the returned object removes only that registration.
    /// Canceling <paramref name="cancellationToken"/> removes the listener, as disposing the returned object does.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event object.</typeparam>
    /// <param name="type">The event to listen for, such as <see cref="ChangeEvent"/>.</param>
    /// <param name="listener">The function to call with each event.</param>
    /// <param name="cancellationToken">The token that removes the listener when canceled.</param>
    /// <returns>An object that removes the listener when disposed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="listener"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled before the listener was added.</exception>
    /// <exception cref="NotSupportedException">The browser does not provide the Screen Orientation API.</exception>
    public async ValueTask<IAsyncDisposable> AddEventListenerAsync<TEvent>(EventType<ScreenOrientation, TEvent> type, Func<TEvent, Task> listener, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(listener);
        cancellationToken.ThrowIfCancellationRequested();

        // Adding and removing listeners changes unlocked state, so these awaits keep the renderer's synchronization context.
        var orientation = await GetReferenceAsync();

        return await type.AddListenerAsync(_listeners, orientation, this, listener, cancellationToken);
    }

    async ValueTask IInternalAsyncDisposal.InternalDisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Listeners need the screen orientation reference to remove themselves, so they go first.
        await ((IInternalAsyncDisposal)_listeners).InternalDisposeAsync();

        // Don't wait for a pending acquisition. It could hold up scope disposal until the JS interop timeout.
        if (_referenceTask is { IsCompletedSuccessfully: true } referenceTask)
        {
            await referenceTask.Result.DisposeAsync().ConfigureAwait(false);
        }
    }

    private Task<IJSObjectReference> GetReferenceAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_referenceTask is null || _referenceTask.IsFaulted || _referenceTask.IsCanceled)
        {
            _referenceTask = AcquireReferenceAsync();
        }

        return _referenceTask;
    }

    private async Task<IJSObjectReference> AcquireReferenceAsync()
    {
        const string NotAvailableMessage = "The Screen Orientation API is not available in this browser.";

        IJSObjectReference? reference;
        try
        {
            reference = await _jsRuntime.GetValueAsync<IJSObjectReference?>("screen.orientation").ConfigureAwait(false);
        }
        catch (JSException ex)
        {
            // Interop throws for a missing screen.orientation, such as in Safari before 16.4.
            throw new NotSupportedException(NotAvailableMessage, ex);
        }

        return reference ?? throw new NotSupportedException(NotAvailableMessage);
    }
}
