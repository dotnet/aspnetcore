// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents a browser Web Storage object.
/// </summary>
public sealed class Storage : IInternalAsyncDisposal
{
    private readonly IJSRuntime _jsRuntime;
    private readonly string _propertyName;
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
    /// <returns>The number of stored items.</returns>
    public async ValueTask<uint> GetLengthAsync()
    {
        var storage = await GetReferenceAsync().ConfigureAwait(false);

        return await storage.GetValueAsync<uint>("length").ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the key at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the key to retrieve.</param>
    /// <returns>The key at <paramref name="index"/>, or <see langword="null"/> if no key exists at that index.</returns>
    public async ValueTask<string?> KeyAsync(uint index)
    {
        var storage = await GetReferenceAsync().ConfigureAwait(false);

        return await storage.InvokeAsync<string?>("key", [index]).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to retrieve.</param>
    /// <returns>The value associated with <paramref name="key"/>, or <see langword="null"/> if the key does not exist.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    public async ValueTask<string?> GetItemAsync(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var storage = await GetReferenceAsync().ConfigureAwait(false);

        return await storage.InvokeAsync<string?>("getItem", [key]).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the value associated with the specified key, replacing any existing value.
    /// </summary>
    /// <param name="key">The key of the value to set.</param>
    /// <param name="value">The value to store.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="value"/> is <see langword="null"/>.</exception>
    public async ValueTask SetItemAsync(string key, string value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        var storage = await GetReferenceAsync().ConfigureAwait(false);
        await storage.InvokeVoidAsync("setItem", key, value).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes the value associated with the specified key, if it exists.
    /// </summary>
    /// <param name="key">The key of the value to remove.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    public async ValueTask RemoveItemAsync(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var storage = await GetReferenceAsync().ConfigureAwait(false);
        await storage.InvokeVoidAsync("removeItem", key).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes all stored values.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    public async ValueTask ClearAsync()
    {
        var storage = await GetReferenceAsync().ConfigureAwait(false);
        await storage.InvokeVoidAsync("clear").ConfigureAwait(false);
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
            _referenceTask = _jsRuntime.GetValueAsync<IJSObjectReference>(_propertyName).AsTask();
        }

        return _referenceTask;
    }
}
