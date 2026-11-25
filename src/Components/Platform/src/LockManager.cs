// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Internal;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents the browser Web Locks API, which coordinates work between the tabs, windows, and workers of the same origin.
/// </summary>
public sealed class LockManager : IInternalAsyncDisposal
{
    // A Lock is a browser object, so the callback receives a copy of its attributes.
    private const string LockShapesJson = """[{"name":null,"mode":null}]""";

    private readonly IJSRuntime _jsRuntime;
    private readonly JSCallbackFactory _callbackFactory;
    private Task<IJSObjectReference>? _referenceTask;
    private bool _disposed;

    internal LockManager(IJSRuntime jsRuntime, JSCallbackFactory callbackFactory)
    {
        _jsRuntime = jsRuntime;
        _callbackFactory = callbackFactory;
    }

    /// <summary>
    /// Requests an exclusive lock with the specified name, waits until it is granted, and calls
    /// <paramref name="callback"/> while it is held. The lock is released when the <see cref="Task"/> that
    /// <paramref name="callback"/> returns completes. An exception thrown by <paramref name="callback"/>
    /// releases the lock and is thrown from this method.
    /// </summary>
    /// <param name="name">The name of the lock.</param>
    /// <param name="callback">The function to call while the lock is held.</param>
    /// <returns>A <see cref="ValueTask"/> that completes after the lock is released.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="callback"/> is <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException">The browser does not provide the Web Locks API for the current page.</exception>
    public async ValueTask RequestAsync(string name, Func<Lock?, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(callback);

        // Creating the callback changes unlocked state, so these awaits keep the renderer's synchronization context.
        var locks = await GetReferenceAsync();

        await _callbackFactory.InvokeWithCallbackAsync(
            callback,
            LockShapesJson,
            function => locks.InvokeVoidAsync("request", name, function).AsTask());
    }

    /// <summary>
    /// Requests a lock with the specified name and options, and calls <paramref name="callback"/> while it is held.
    /// The lock is released when the <see cref="Task"/> that <paramref name="callback"/> returns completes.
    /// If <see cref="LockOptions.IfAvailable"/> is <see langword="true"/> and the lock is not available,
    /// <paramref name="callback"/> is called with <see langword="null"/> instead.
    /// An exception thrown by <paramref name="callback"/> releases the lock and is thrown from this method.
    /// </summary>
    /// <param name="name">The name of the lock.</param>
    /// <param name="options">The options of the request.</param>
    /// <param name="callback">The function to call while the lock is held, or with <see langword="null"/> if the lock was not granted.</param>
    /// <returns>A <see cref="ValueTask"/> that completes after the lock is released.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/>, <paramref name="options"/>, or <paramref name="callback"/> is <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException">The browser does not provide the Web Locks API for the current page.</exception>
    [DynamicDependency(LinkerFlags.JsonSerialized, typeof(LockOptions))]
    public async ValueTask RequestAsync(string name, LockOptions options, Func<Lock?, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(callback);

        // Creating the callback changes unlocked state, so these awaits keep the renderer's synchronization context.
        var locks = await GetReferenceAsync();

        await _callbackFactory.InvokeWithCallbackAsync(
            callback,
            LockShapesJson,
            function => locks.InvokeVoidAsync("request", name, options, function).AsTask());
    }

    /// <summary>
    /// Gets the locks of the origin that are held and the lock requests that are waiting.
    /// </summary>
    /// <returns>The held locks and the pending lock requests of the origin.</returns>
    /// <exception cref="NotSupportedException">The browser does not provide the Web Locks API for the current page.</exception>
    [DynamicDependency(LinkerFlags.JsonSerialized, typeof(LockInfo))]
    public async ValueTask<LockManagerSnapshot> QueryAsync()
    {
        var locks = await GetReferenceAsync().ConfigureAwait(false);

        var snapshot = await locks.InvokeAsync<LockManagerSnapshotPayload>("query").ConfigureAwait(false);

        return snapshot.ToLockManagerSnapshot();
    }

    async ValueTask IInternalAsyncDisposal.InternalDisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

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
        const string NotAvailableMessage = "The Web Locks API is not available for the current page.";

        IJSObjectReference? reference;
        try
        {
            reference = await _jsRuntime.GetValueAsync<IJSObjectReference?>("navigator.locks").ConfigureAwait(false);
        }
        catch (JSException ex)
        {
            // Interop throws for a missing navigator.locks, such as on an insecure page.
            throw new NotSupportedException(NotAvailableMessage, ex);
        }

        return reference ?? throw new NotSupportedException(NotAvailableMessage);
    }
}
