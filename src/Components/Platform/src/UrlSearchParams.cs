// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents a live browser URL query parameter collection.
/// </summary>
public sealed class UrlSearchParams : IAsyncDisposable
{
    private readonly IJSObjectReference _reference;
    private readonly object _lock = new();
    private bool _disposed;

    internal UrlSearchParams(IJSObjectReference reference)
    {
        _reference = reference;
    }

    /// <summary>
    /// Gets the number of query parameters.
    /// </summary>
    public ValueTask<uint> GetSizeAsync()
    {
        return GetReference().GetValueAsync<uint>("size", CancellationToken.None);
    }

    /// <summary>
    /// Appends a query parameter.
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
    /// Deletes query parameters with the specified name and optional value.
    /// </summary>
    public ValueTask DeleteAsync(
        string name,
        string? value = null)
    {
        ArgumentNullException.ThrowIfNull(name);

        return value is null
            ? GetReference().InvokeVoidAsync("delete", name)
            : GetReference().InvokeVoidAsync("delete", name, value);
    }

    /// <summary>
    /// Gets the first value for a query parameter.
    /// </summary>
    public ValueTask<string?> GetAsync(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return GetReference().InvokeAsync<string?>("get", [name]);
    }

    /// <summary>
    /// Gets all values for a query parameter.
    /// </summary>
    public ValueTask<string[]> GetAllAsync(
        string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return GetReference().InvokeAsync<string[]>("getAll", [name]);
    }

    /// <summary>
    /// Determines whether a query parameter with the optional value exists.
    /// </summary>
    public ValueTask<bool> HasAsync(
        string name,
        string? value = null)
    {
        ArgumentNullException.ThrowIfNull(name);

        return value is null
            ? GetReference().InvokeAsync<bool>("has", [name])
            : GetReference().InvokeAsync<bool>("has", [name, value]);
    }

    /// <summary>
    /// Sets the value of a query parameter.
    /// </summary>
    public ValueTask SetAsync(
        string name,
        string value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);

        return GetReference().InvokeVoidAsync("set", name, value);
    }

    /// <summary>
    /// Sorts query parameters by name.
    /// </summary>
    public ValueTask SortAsync()
    {
        return GetReference().InvokeVoidAsync("sort");
    }

    /// <summary>
    /// Converts the query parameters to their serialized string form.
    /// </summary>
    public ValueTask<string> ToStringAsync()
    {
        return GetReference().InvokeAsync<string>("toString", []);
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
