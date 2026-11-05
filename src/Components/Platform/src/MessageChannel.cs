// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents a browser channel with two connected message ports.
/// </summary>
public sealed class MessageChannel : IAsyncDisposable
{
    private readonly IJSObjectReference _reference;
    private readonly MessagePort _port1;
    private readonly MessagePort _port2;
    private bool _disposed;

    internal MessageChannel(IJSRuntime jsRuntime, IJSObjectReference reference)
    {
        _reference = reference;
        _port1 = new MessagePort(jsRuntime, reference, "port1");
        _port2 = new MessagePort(jsRuntime, reference, "port2");
    }

    /// <summary>
    /// Gets the first port, which is owned by this channel and should not be disposed separately.
    /// </summary>
    public MessagePort Port1
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            return _port1;
        }
    }

    /// <summary>
    /// Gets the second port, which is owned by this channel and should not be disposed separately.
    /// </summary>
    public MessagePort Port2
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            return _port2;
        }
    }

    /// <summary>
    /// Closes ports that have been used and releases this channel's resources.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            await _port1.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            try
            {
                await _port2.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                await _reference.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
