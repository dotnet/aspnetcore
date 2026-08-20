// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents browser Fetch headers.
/// </summary>
public sealed class Headers : IAsyncDisposable
{
    private readonly IJSObjectReference _reference;
    private readonly object _lock = new();
    private bool _disposed;

    internal Headers(IJSObjectReference reference)
    {
        _reference = reference;
    }

    /// <summary>
    /// Appends a header value.
    /// </summary>
    public ValueTask AppendAsync(
        string name,
        string value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);

        return GetReference().InvokeVoidAsync("append", name, value);
    }

    /// <summary>
    /// Deletes a header.
    /// </summary>
    public ValueTask DeleteAsync(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return GetReference().InvokeVoidAsync("delete", name);
    }

    /// <summary>
    /// Gets a header value.
    /// </summary>
    public ValueTask<string?> GetAsync(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return GetReference().InvokeAsync<string?>("get", [name]);
    }

    /// <summary>
    /// Determines whether a header exists.
    /// </summary>
    public ValueTask<bool> HasAsync(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return GetReference().InvokeAsync<bool>("has", [name]);
    }

    /// <summary>
    /// Sets a header value.
    /// </summary>
    public ValueTask SetAsync(
        string name,
        string value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);

        return GetReference().InvokeVoidAsync("set", name, value);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        await _reference.DisposeAsync().ConfigureAwait(false);
    }

    private IJSObjectReference GetReference()
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            return _reference;
        }
    }
}
