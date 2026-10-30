// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Connects a <see cref="CancellationToken"/> to a browser <c>AbortController</c>, so that canceling the token
/// aborts the browser operation that received <see cref="Signal"/>.
/// </summary>
/// <remarks>
/// A token given to <see cref="IJSRuntime"/> only stops .NET from waiting, and a reference the browser returns later
/// is never disposed. Call such an operation with <see cref="CancellationToken.None"/> and cancel it only through this link.
/// </remarks>
internal sealed class AbortLink : IAsyncDisposable
{
    private const string CanceledMessage = "The operation was canceled.";

    private readonly IJSObjectReference _controller;
    private CancellationTokenRegistration _registration;
    private bool _disposed;

    private AbortLink(IJSObjectReference controller, IJSObjectReference signal)
    {
        _controller = controller;
        Signal = signal;
    }

    /// <summary>
    /// Gets the <c>AbortSignal</c> to pass to the browser operation.
    /// </summary>
    public IJSObjectReference Signal { get; }

    /// <summary>
    /// Creates a link for <paramref name="cancellationToken"/>.
    /// </summary>
    /// <param name="jsRuntime">The JS runtime of the current scope.</param>
    /// <param name="cancellationToken">The token that aborts the browser operation.</param>
    /// <returns>
    /// The link, or <see langword="null"/> if <paramref name="cancellationToken"/> cannot be canceled.
    /// </returns>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> is already canceled.</exception>
    public static async ValueTask<AbortLink?> CreateAsync(IJSRuntime jsRuntime, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!cancellationToken.CanBeCanceled)
        {
            return null;
        }

        var controller = await jsRuntime.InvokeConstructorAsync("AbortController").ConfigureAwait(false);
        IJSObjectReference signal;
        try
        {
            signal = await controller.GetValueAsync<IJSObjectReference>("signal").ConfigureAwait(false);
        }
        catch
        {
            await controller.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        var link = new AbortLink(controller, signal);
        if (cancellationToken.IsCancellationRequested)
        {
            await link.DisposeAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }

        // Runs the callback at once if the token is canceled between the check above and this call.
        link._registration = cancellationToken.Register(static state => ((AbortLink)state!).Abort(), link);

        return link;
    }

    /// <summary>
    /// Creates the exception to throw when a browser operation failed after <paramref name="cancellationToken"/>
    /// was canceled.
    /// </summary>
    /// <remarks>
    /// Use it whenever cancellation was requested, whatever the browser reported. The error text of an abort
    /// differs by browser, and a failure that races with the abort is reported as canceled as well.
    /// </remarks>
    /// <param name="innerException">The exception that the browser operation threw.</param>
    /// <param name="cancellationToken">The canceled token.</param>
    /// <returns>The exception to throw.</returns>
    public static OperationCanceledException CreateCanceledException(Exception innerException, CancellationToken cancellationToken)
        => new(CanceledMessage, innerException, cancellationToken);

    /// <summary>
    /// Stops listening to the token and releases the browser objects.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Waits for a running callback, so no abort is sent through a disposed reference.
        await _registration.DisposeAsync().ConfigureAwait(false);
        await _controller.DisposeAsync().ConfigureAwait(false);
        await Signal.DisposeAsync().ConfigureAwait(false);
    }

    // Runs on the thread that cancels the token, which in Interactive Server may be outside the circuit's
    // sync context. It only starts the call and touches no state, so it needs no lock.
    private void Abort() => _ = AbortAsync();

    private async Task AbortAsync()
    {
        try
        {
            await _controller.InvokeVoidAsync("abort").ConfigureAwait(false);
        }
        catch
        {
            // Best effort. The circuit may be gone, or the operation may have finished and disposed the link.
        }
    }
}
