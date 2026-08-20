// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents a browser URL object.
/// </summary>
public sealed class Url : IAsyncDisposable
{
    private readonly IJSObjectReference _reference;
    private readonly object _lock = new();
    private Task<UrlSearchParams>? _searchParamsTask;
    private bool _disposed;

    internal Url(IJSObjectReference reference)
    {
        _reference = reference;
    }

    /// <summary>
    /// Gets the serialized URL.
    /// </summary>
    public ValueTask<string> GetHrefAsync()
        => GetStringAsync("href");

    /// <summary>
    /// Sets the serialized URL.
    /// </summary>
    public ValueTask SetHrefAsync(string value)
        => SetStringAsync("href", value);

    /// <summary>
    /// Gets the URL origin.
    /// </summary>
    public ValueTask<string> GetOriginAsync()
        => GetStringAsync("origin");

    /// <summary>
    /// Gets the URL protocol.
    /// </summary>
    public ValueTask<string> GetProtocolAsync()
        => GetStringAsync("protocol");

    /// <summary>
    /// Sets the URL protocol.
    /// </summary>
    public ValueTask SetProtocolAsync(string value)
        => SetStringAsync("protocol", value);

    /// <summary>
    /// Gets the URL username.
    /// </summary>
    public ValueTask<string> GetUsernameAsync()
        => GetStringAsync("username");

    /// <summary>
    /// Sets the URL username.
    /// </summary>
    public ValueTask SetUsernameAsync(string value)
        => SetStringAsync("username", value);

    /// <summary>
    /// Gets the URL password.
    /// </summary>
    public ValueTask<string> GetPasswordAsync()
        => GetStringAsync("password");

    /// <summary>
    /// Sets the URL password.
    /// </summary>
    public ValueTask SetPasswordAsync(string value)
        => SetStringAsync("password", value);

    /// <summary>
    /// Gets the URL host.
    /// </summary>
    public ValueTask<string> GetHostAsync()
        => GetStringAsync("host");

    /// <summary>
    /// Sets the URL host.
    /// </summary>
    public ValueTask SetHostAsync(string value)
        => SetStringAsync("host", value);

    /// <summary>
    /// Gets the URL host name.
    /// </summary>
    public ValueTask<string> GetHostnameAsync()
        => GetStringAsync("hostname");

    /// <summary>
    /// Sets the URL host name.
    /// </summary>
    public ValueTask SetHostnameAsync(string value)
        => SetStringAsync("hostname", value);

    /// <summary>
    /// Gets the URL port.
    /// </summary>
    public ValueTask<string> GetPortAsync()
        => GetStringAsync("port");

    /// <summary>
    /// Sets the URL port.
    /// </summary>
    public ValueTask SetPortAsync(string value)
        => SetStringAsync("port", value);

    /// <summary>
    /// Gets the URL path.
    /// </summary>
    public ValueTask<string> GetPathnameAsync()
        => GetStringAsync("pathname");

    /// <summary>
    /// Sets the URL path.
    /// </summary>
    public ValueTask SetPathnameAsync(string value)
        => SetStringAsync("pathname", value);

    /// <summary>
    /// Gets the URL query including its leading question mark.
    /// </summary>
    public ValueTask<string> GetSearchAsync()
        => GetStringAsync("search");

    /// <summary>
    /// Sets the URL query.
    /// </summary>
    public ValueTask SetSearchAsync(string value)
        => SetStringAsync("search", value);

    /// <summary>
    /// Gets the URL fragment including its leading number sign.
    /// </summary>
    public ValueTask<string> GetHashAsync()
        => GetStringAsync("hash");

    /// <summary>
    /// Sets the URL fragment.
    /// </summary>
    public ValueTask SetHashAsync(string value)
        => SetStringAsync("hash", value);

    /// <summary>
    /// Gets the live query parameter collection for this URL.
    /// </summary>
    public async ValueTask<UrlSearchParams> GetSearchParamsAsync()
    {
        Task<UrlSearchParams> searchParamsTask;

        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _searchParamsTask ??= CreateSearchParamsAsync(_reference);
            searchParamsTask = _searchParamsTask;
        }

        try
        {
            return await searchParamsTask.ConfigureAwait(false);
        }
        catch
        {
            lock (_lock)
            {
                if (ReferenceEquals(_searchParamsTask, searchParamsTask))
                {
                    _searchParamsTask = null;
                }
            }

            throw;
        }
    }

    /// <summary>
    /// Converts the URL to its serialized string form.
    /// </summary>
    public ValueTask<string> ToStringAsync()
    {
        return GetReference().InvokeAsync<string>("toString", []);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        Task<UrlSearchParams>? searchParamsTask;

        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            searchParamsTask = _searchParamsTask;
        }

        if (searchParamsTask is not null &&
            !searchParamsTask.IsCanceled &&
            !searchParamsTask.IsFaulted)
        {
            var searchParams = await searchParamsTask.ConfigureAwait(false);
            await searchParams.DisposeAsync().ConfigureAwait(false);
        }

        await _reference.DisposeAsync().ConfigureAwait(false);
    }

    private static async Task<UrlSearchParams> CreateSearchParamsAsync(
        IJSObjectReference reference)
    {
        var searchParamsReference = await reference
            .GetValueAsync<IJSObjectReference>("searchParams", CancellationToken.None)
            .ConfigureAwait(false);

        return new UrlSearchParams(searchParamsReference);
    }

    private ValueTask<string> GetStringAsync(string propertyName)
    {
        return GetReference().GetValueAsync<string>(propertyName, CancellationToken.None);
    }

    private ValueTask SetStringAsync(
        string propertyName,
        string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return GetReference().SetValueAsync(propertyName, value, CancellationToken.None);
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
