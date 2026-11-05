// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.JSInterop;

/// <summary>
/// Owns the reference that projects a .NET cancellation token to a JavaScript <c>AbortSignal</c>.
/// </summary>
public sealed class DotNetCancellationTokenReference : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly DotNetObjectReference<CancellationTokenObserver> _reference;
    private readonly IJSObjectReference _signal;
    private Task? _disposal;

    private DotNetCancellationTokenReference(IJSRuntime jsRuntime, DotNetObjectReference<CancellationTokenObserver> reference, IJSObjectReference signal)
    {
        _jsRuntime = jsRuntime;
        _reference = reference;
        _signal = signal;
    }

    internal IJSObjectReference Signal
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposal is not null, this);
            return _signal;
        }
    }

    /// <summary>
    /// Creates a reference that can be passed as an ordinary interop argument to supply a native JavaScript <c>AbortSignal</c>.
    /// </summary>
    /// <param name="jsRuntime">The runtime that will receive the reference.</param>
    /// <param name="cancellationToken">The token to observe. It does not cancel creation of the reference or an interop invocation.</param>
    /// <returns>A reference owned by the caller. Dispose it when JavaScript no longer needs to observe the token.</returns>
    /// <remarks>The reference can be reused across invocations. Disposing it stops observation without canceling the token or aborting the signal.</remarks>
    public static async ValueTask<DotNetCancellationTokenReference> CreateAsync(IJSRuntime jsRuntime, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(jsRuntime);
        var observer = new CancellationTokenObserver(cancellationToken);
        var reference = DotNetObjectReference.Create(observer);
        try
        {
            var signal = await jsRuntime.InvokeAsync<IJSObjectReference>(
                "DotNet.createAbortSignal", CancellationToken.None, [reference, cancellationToken.IsCancellationRequested]);
            return new DotNetCancellationTokenReference(jsRuntime, reference, signal);
        }
        catch
        {
            observer.Dispose();
            reference.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Stops observing the token and releases the JavaScript and .NET references without canceling application work.
    /// </summary>
    /// <returns>A task that completes after pending notification and reference cleanup.</returns>
    public ValueTask DisposeAsync() => new(_disposal ??= DisposeCoreAsync());

    private async Task DisposeCoreAsync()
    {
        _reference.Value.Dispose();
        try
        {
            await _jsRuntime.InvokeVoidAsync("DotNet.disposeAbortSignal", CancellationToken.None, _signal);
        }
        catch (JSDisconnectedException)
        {
            // The browser resources are already unavailable after disconnection.
        }
        finally
        {
            try
            {
                await _signal.DisposeAsync();
            }
            finally
            {
                _reference.Dispose();
            }
        }
    }
}
