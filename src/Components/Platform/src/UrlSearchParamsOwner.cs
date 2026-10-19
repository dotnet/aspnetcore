// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Owns a browser URLSearchParams object created by the application.
/// </summary>
public sealed class UrlSearchParamsOwner : IAsyncDisposable
{
    private readonly UrlSearchParams _value;
    private bool _disposed;

    internal UrlSearchParamsOwner(UrlSearchParams value)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the owned query parameter collection.
    /// </summary>
    public UrlSearchParams Value
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            return _value;
        }
    }

    /// <summary>
    /// Releases the browser URLSearchParams object.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await ((IInternalAsyncDisposal)_value).InternalDisposeAsync().ConfigureAwait(false);
    }
}
