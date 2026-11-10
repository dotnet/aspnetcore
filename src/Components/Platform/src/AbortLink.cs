// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Connects a <see cref="CancellationToken"/> to a browser <c>AbortController</c>, so that canceling the token
/// aborts the browser operation that received <see cref="Signal"/>.
/// </summary>
/// <remarks>
/// A token given to <see cref="IJSRuntime"/> only stops .NET from waiting, and a reference the browser returns later
/// is never disposed. Call such an operation with <see cref="CancellationToken.None"/> and cancel it only through this link.
/// Create it with <see cref="AbortLinkFactory.CreateAsync(CancellationToken)"/>.
/// </remarks>
internal sealed class AbortLink : IAsyncDisposable
{
    private const string CanceledMessage = "The operation was canceled.";

    private readonly AbortLinkFactory _owner;
    private readonly IJSObjectReference _controller;
    private readonly ILogger _logger;
    private CancellationTokenRegistration _registration;
    private bool _disposed;

    internal AbortLink(AbortLinkFactory owner, IJSObjectReference controller, IJSObjectReference signal, ILogger logger)
    {
        _owner = owner;
        _controller = controller;
        Signal = signal;
        _logger = logger;
    }

    /// <summary>
    /// Gets the <c>AbortSignal</c> to pass to the browser operation.
    /// </summary>
    public IJSObjectReference Signal { get; }

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
        _owner.Remove(this);

        // Waits for a running callback, so no abort is sent through a disposed reference.
        await _registration.DisposeAsync();
        await _controller.DisposeAsync();
        await Signal.DisposeAsync();
    }

    // Runs the callback at once if the token is already canceled.
    internal void AbortWhenCanceled(CancellationToken cancellationToken)
        => _registration = cancellationToken.Register(static state => ((AbortLink)state!).Abort(), this);

    // Runs on the thread that cancels the token, which in Interactive Server may be outside the circuit's
    // synchronization context. It only sends the call and touches no state of the scope, so it needs no lock.
    private void Abort() => _ = AbortAsync();

    private async Task AbortAsync()
    {
        try
        {
            await _controller.InvokeVoidAsync("abort");
        }
        catch (JSDisconnectedException)
        {
            // The circuit has ended, and the browser operation with it.
        }
        catch (Exception ex)
        {
            AbortLinkFactory.Log.AbortFailed(_logger, ex);
        }
    }
}
