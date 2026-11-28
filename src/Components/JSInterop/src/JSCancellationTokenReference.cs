// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.JSInterop;

/// <summary>
/// Owns a cancellation token projected from a JavaScript <c>AbortSignal</c>.
/// </summary>
/// <remarks>
/// This type supports runtime-owned marshalling of JavaScript signal arguments.
/// Operations must run on the owning runtime's dispatcher or synchronization context.
/// </remarks>
public sealed class JSCancellationTokenReference : IAsyncDisposable
{
    private readonly CancellationTokenSource _source = new();
    private readonly IJSObjectReference _signal;
    private readonly DotNetObjectReference<JSCancellationTokenReference> _reference;
    private Task? _cancellation;
    private Task? _disposal;

    private JSCancellationTokenReference(IJSObjectReference signal, bool isAborted)
    {
        _signal = signal;
        _reference = DotNetObjectReference.Create(this);
        if (isAborted)
        {
            _source.Cancel();
        }
    }

    /// <summary>
    /// Gets the token that observes the referenced JavaScript signal.
    /// </summary>
    public CancellationToken Token => _source.Token;

    /// <summary>
    /// Creates the .NET reference used by the JavaScript cancellation-reference factory.
    /// </summary>
    /// <param name="signal">The JavaScript owner of the signal and its subscription.</param>
    /// <param name="isAborted">Whether the signal is already aborted.</param>
    /// <returns>The reference that supplies a token to interop parameters declared as <see cref="CancellationToken"/>.</returns>
    [JSInvokable("CreateCancellationTokenReference")]
    public static DotNetObjectReference<JSCancellationTokenReference> Create(IJSObjectReference signal, bool isAborted)
    {
        ArgumentNullException.ThrowIfNull(signal);
        return new JSCancellationTokenReference(signal, isAborted)._reference;
    }

    /// <summary>
    /// Notifies the token that the JavaScript signal has aborted.
    /// </summary>
    /// <returns>A task that completes after registered cancellation callbacks finish.</returns>
    [JSInvokable]
    public Task CancelAsync()
    {
        ObjectDisposedException.ThrowIf(_disposal is not null, this);
        return _cancellation ??= _source.CancelAsync();
    }

    /// <summary>
    /// Removes the signal subscription and releases the references without requesting cancellation.
    /// </summary>
    /// <returns>A task that completes after notification and cleanup.</returns>
    [JSInvokable]
    public ValueTask DisposeAsync()
    {
        return new(_disposal ??= DisposeCoreAsync(_cancellation ?? Task.CompletedTask));
    }

    private async Task DisposeCoreAsync(Task cancellation)
    {
        try
        {
            await cancellation;
        }
        finally
        {
            try
            {
                await _signal.InvokeVoidAsync("dispose");
            }
            catch (JSDisconnectedException)
            {
                // The browser subscription is already unavailable after disconnection.
            }
            finally
            {
                // Tokens retained by application code outlive the interop references.
                _reference.Dispose();
                await _signal.DisposeAsync();
            }
        }
    }

    internal void DisposeManaged() => _reference.Dispose();
}
