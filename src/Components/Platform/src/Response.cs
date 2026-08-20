// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents a browser Fetch response.
/// </summary>
public sealed class Response : IAsyncDisposable
{
    private readonly IJSObjectReference _reference;
    private readonly object _lock = new();
    private Task<Headers>? _headersTask;
    private bool _disposed;

    internal Response(IJSObjectReference reference)
    {
        _reference = reference;
    }

    /// <summary>
    /// Gets whether the response has a successful status.
    /// </summary>
    public ValueTask<bool> GetOkAsync()
    {
        return GetReference().GetValueAsync<bool>("ok", CancellationToken.None);
    }

    /// <summary>
    /// Gets the response status code.
    /// </summary>
    public ValueTask<ushort> GetStatusAsync()
    {
        return GetReference().GetValueAsync<ushort>("status", CancellationToken.None);
    }

    /// <summary>
    /// Gets the response status text.
    /// </summary>
    public ValueTask<string> GetStatusTextAsync()
    {
        return GetReference().GetValueAsync<string>("statusText", CancellationToken.None);
    }

    /// <summary>
    /// Gets the final response URL.
    /// </summary>
    public ValueTask<string> GetUrlAsync()
    {
        return GetReference().GetValueAsync<string>("url", CancellationToken.None);
    }

    /// <summary>
    /// Gets the live response headers collection.
    /// </summary>
    public async ValueTask<Headers> GetHeadersAsync()
    {
        Task<Headers> headersTask;

        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _headersTask ??= CreateHeadersAsync(_reference);
            headersTask = _headersTask;
        }

        try
        {
            return await headersTask.ConfigureAwait(false);
        }
        catch
        {
            lock (_lock)
            {
                if (ReferenceEquals(_headersTask, headersTask))
                {
                    _headersTask = null;
                }
            }

            throw;
        }
    }

    /// <summary>
    /// Reads the response body as text.
    /// </summary>
    public ValueTask<string> TextAsync()
    {
        return GetReference().InvokeAsync<string>("text", []);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        Task<Headers>? headersTask;

        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            headersTask = _headersTask;
        }

        if (headersTask is not null && !headersTask.IsCanceled && !headersTask.IsFaulted)
        {
            var headers = await headersTask.ConfigureAwait(false);
            await headers.DisposeAsync().ConfigureAwait(false);
        }

        await _reference.DisposeAsync().ConfigureAwait(false);
    }

    private static async Task<Headers> CreateHeadersAsync(
        IJSObjectReference reference)
    {
        var headersReference = await reference
            .GetValueAsync<IJSObjectReference>("headers", CancellationToken.None)
            .ConfigureAwait(false);

        return new Headers(headersReference);
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
