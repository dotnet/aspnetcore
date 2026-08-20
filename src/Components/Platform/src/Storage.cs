// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents a browser Web Storage object.
/// </summary>
public sealed class Storage : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly string _propertyName;
    private readonly object _lock = new();
    private Task<IJSObjectReference>? _referenceTask;
    private bool _disposed;

    internal Storage(IJSRuntime jsRuntime, string propertyName)
    {
        _jsRuntime = jsRuntime;
        _propertyName = propertyName;
    }

    /// <summary>
    /// Gets the number of stored items.
    /// </summary>
    public async ValueTask<uint> GetLengthAsync()
    {
        var storage = await GetReferenceAsync().ConfigureAwait(false);

        return await storage
            .GetValueAsync<uint>("length", CancellationToken.None)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the key at the specified index.
    /// </summary>
    public async ValueTask<string?> KeyAsync(uint index)
    {
        var storage = await GetReferenceAsync().ConfigureAwait(false);

        return await storage.InvokeAsync<string?>("key", [index]).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    public async ValueTask<string?> GetItemAsync(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var storage = await GetReferenceAsync().ConfigureAwait(false);

        return await storage.InvokeAsync<string?>("getItem", [key]).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the value associated with the specified key.
    /// </summary>
    public async ValueTask SetItemAsync(string key, string value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        var storage = await GetReferenceAsync().ConfigureAwait(false);
        await storage.InvokeVoidAsync("setItem", key, value).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes the value associated with the specified key.
    /// </summary>
    public async ValueTask RemoveItemAsync(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var storage = await GetReferenceAsync().ConfigureAwait(false);
        await storage.InvokeVoidAsync("removeItem", key).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes all stored values.
    /// </summary>
    public async ValueTask ClearAsync()
    {
        var storage = await GetReferenceAsync().ConfigureAwait(false);
        await storage.InvokeVoidAsync("clear").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        Task<IJSObjectReference>? referenceTask;

        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            referenceTask = _referenceTask;
        }

        if (referenceTask is null || referenceTask.IsCanceled || referenceTask.IsFaulted)
        {
            return;
        }

        var reference = await referenceTask.ConfigureAwait(false);
        await reference.DisposeAsync().ConfigureAwait(false);
    }

    private Task<IJSObjectReference> GetReferenceAsync()
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _referenceTask ??= _jsRuntime.GetValueAsync<IJSObjectReference>(_propertyName).AsTask();

            return _referenceTask;
        }
    }
}
