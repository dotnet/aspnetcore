// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents the browser Storage API, which reports and controls how the browser keeps the data of the current origin.
/// </summary>
public sealed class StorageManager : IInternalAsyncDisposal
{
    private readonly IJSRuntime _jsRuntime;
    private Task<IJSObjectReference>? _referenceTask;
    private bool _disposed;

    internal StorageManager(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Gets a value indicating whether the browser keeps the data of the current origin until the user deletes it.
    /// </summary>
    /// <returns><see langword="true"/> if the storage of the current origin is persistent; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="NotSupportedException">The browser does not provide the Storage API for the current page.</exception>
    public async ValueTask<bool> PersistedAsync()
    {
        var storageManager = await GetReferenceAsync().ConfigureAwait(false);

        return await storageManager.InvokeAsync<bool>("persisted").ConfigureAwait(false);
    }

    /// <summary>
    /// Asks the browser to keep the data of the current origin until the user deletes it, instead of clearing it under storage pressure.
    /// </summary>
    /// <remarks>
    /// Some browsers ask the user for permission, and others decide by themselves.
    /// </remarks>
    /// <returns><see langword="true"/> if the storage of the current origin is persistent; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="NotSupportedException">The browser does not provide the Storage API for the current page.</exception>
    public async ValueTask<bool> PersistAsync()
    {
        var storageManager = await GetReferenceAsync().ConfigureAwait(false);

        return await storageManager.InvokeAsync<bool>("persist").ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an estimate of how much storage the current origin uses and how much it can use.
    /// </summary>
    /// <returns>The estimated usage and quota of the current origin.</returns>
    /// <exception cref="NotSupportedException">The browser does not provide the Storage API for the current page.</exception>
    public async ValueTask<StorageEstimate> EstimateAsync()
    {
        var storageManager = await GetReferenceAsync().ConfigureAwait(false);

        return await storageManager.InvokeAsync<StorageEstimate>("estimate").ConfigureAwait(false);
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
            _referenceTask = AcquireReferenceAsync();
        }

        return _referenceTask;
    }

    private async Task<IJSObjectReference> AcquireReferenceAsync()
    {
        const string NotAvailableMessage = "The Storage API is not available for the current page.";

        IJSObjectReference? reference;
        try
        {
            reference = await _jsRuntime.GetValueAsync<IJSObjectReference?>("navigator.storage").ConfigureAwait(false);
        }
        catch (JSException ex)
        {
            // Interop throws for a missing navigator.storage, such as on an insecure page.
            throw new NotSupportedException(NotAvailableMessage, ex);
        }

        return reference ?? throw new NotSupportedException(NotAvailableMessage);
    }
}
