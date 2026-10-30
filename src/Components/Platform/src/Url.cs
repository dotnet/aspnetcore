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
    private UrlSearchParams? _searchParams;
    private bool _disposed;

    internal Url(IJSObjectReference reference)
    {
        _reference = reference;
    }

    /// <summary>
    /// Gets the URL query parameter collection.
    /// </summary>
    /// <remarks>
    /// This instance is owned by the URL. Do not dispose it separately; it is released when the URL is disposed.
    /// </remarks>
    public UrlSearchParams SearchParams
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            return _searchParams ??= new UrlSearchParams(_reference, "searchParams");
        }
    }

    /// <summary>
    /// Gets the serialized URL.
    /// </summary>
    /// <returns>The serialized URL.</returns>
    public ValueTask<string> GetHrefAsync()
        => GetStringAsync("href");

    /// <summary>
    /// Sets the serialized URL.
    /// </summary>
    /// <param name="value">The serialized URL.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public ValueTask SetHrefAsync(string value)
        => SetStringAsync("href", value);

    /// <summary>
    /// Gets the URL origin.
    /// </summary>
    /// <returns>The URL origin.</returns>
    public ValueTask<string> GetOriginAsync()
        => GetStringAsync("origin");

    /// <summary>
    /// Gets the URL protocol.
    /// </summary>
    /// <returns>The URL protocol.</returns>
    public ValueTask<string> GetProtocolAsync()
        => GetStringAsync("protocol");

    /// <summary>
    /// Sets the URL protocol.
    /// </summary>
    /// <param name="value">The URL protocol.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public ValueTask SetProtocolAsync(string value)
        => SetStringAsync("protocol", value);

    /// <summary>
    /// Gets the URL username.
    /// </summary>
    /// <returns>The URL username.</returns>
    public ValueTask<string> GetUsernameAsync()
        => GetStringAsync("username");

    /// <summary>
    /// Sets the URL username.
    /// </summary>
    /// <param name="value">The URL username.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public ValueTask SetUsernameAsync(string value)
        => SetStringAsync("username", value);

    /// <summary>
    /// Gets the URL password.
    /// </summary>
    /// <returns>The URL password.</returns>
    public ValueTask<string> GetPasswordAsync()
        => GetStringAsync("password");

    /// <summary>
    /// Sets the URL password.
    /// </summary>
    /// <param name="value">The URL password.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public ValueTask SetPasswordAsync(string value)
        => SetStringAsync("password", value);

    /// <summary>
    /// Gets the URL host.
    /// </summary>
    /// <returns>The URL host.</returns>
    public ValueTask<string> GetHostAsync()
        => GetStringAsync("host");

    /// <summary>
    /// Sets the URL host.
    /// </summary>
    /// <param name="value">The URL host.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public ValueTask SetHostAsync(string value)
        => SetStringAsync("host", value);

    /// <summary>
    /// Gets the URL host name.
    /// </summary>
    /// <returns>The URL host name.</returns>
    public ValueTask<string> GetHostnameAsync()
        => GetStringAsync("hostname");

    /// <summary>
    /// Sets the URL host name.
    /// </summary>
    /// <param name="value">The URL host name.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public ValueTask SetHostnameAsync(string value)
        => SetStringAsync("hostname", value);

    /// <summary>
    /// Gets the URL port.
    /// </summary>
    /// <returns>The URL port.</returns>
    public ValueTask<string> GetPortAsync()
        => GetStringAsync("port");

    /// <summary>
    /// Sets the URL port.
    /// </summary>
    /// <param name="value">The URL port.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public ValueTask SetPortAsync(string value)
        => SetStringAsync("port", value);

    /// <summary>
    /// Gets the URL path.
    /// </summary>
    /// <returns>The URL path.</returns>
    public ValueTask<string> GetPathnameAsync()
        => GetStringAsync("pathname");

    /// <summary>
    /// Sets the URL path.
    /// </summary>
    /// <param name="value">The URL path.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public ValueTask SetPathnameAsync(string value)
        => SetStringAsync("pathname", value);

    /// <summary>
    /// Gets the URL query, including its leading question mark.
    /// </summary>
    /// <returns>The URL query.</returns>
    public ValueTask<string> GetSearchAsync()
        => GetStringAsync("search");

    /// <summary>
    /// Sets the URL query.
    /// </summary>
    /// <param name="value">The URL query.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public ValueTask SetSearchAsync(string value)
        => SetStringAsync("search", value);

    /// <summary>
    /// Gets the URL fragment, including its leading number sign.
    /// </summary>
    /// <returns>The URL fragment.</returns>
    public ValueTask<string> GetHashAsync()
        => GetStringAsync("hash");

    /// <summary>
    /// Sets the URL fragment.
    /// </summary>
    /// <param name="value">The URL fragment.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public ValueTask SetHashAsync(string value)
        => SetStringAsync("hash", value);

    /// <summary>
    /// Converts the URL to its serialized string form.
    /// </summary>
    /// <returns>The serialized URL.</returns>
    public ValueTask<string> ToStringAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _reference.InvokeAsync<string>("toString");
    }

    /// <summary>
    /// Converts the URL to the string value used during JSON serialization.
    /// </summary>
    /// <returns>The serialized URL.</returns>
    public ValueTask<string> ToJsonAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _reference.InvokeAsync<string>("toJSON");
    }

    /// <summary>
    /// Releases the browser URL.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_searchParams is not null)
        {
            await ((IInternalAsyncDisposal)_searchParams).InternalDisposeAsync().ConfigureAwait(false);
        }

        await _reference.DisposeAsync().ConfigureAwait(false);
    }

    private ValueTask<string> GetStringAsync(string propertyName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _reference.GetValueAsync<string>(propertyName);
    }

    private ValueTask SetStringAsync(string propertyName, string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _reference.SetValueAsync(propertyName, value);
    }
}
