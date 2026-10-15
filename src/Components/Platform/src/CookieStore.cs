// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Internal;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents the browser Cookie Store API for the current page.
/// </summary>
public sealed class CookieStore : IInternalAsyncDisposal
{
    // The change event crosses by value, and CookieListItem is a dictionary, so the lists are copied whole.
    private const string OnChangeShapes = """[{"changed":null,"deleted":null}]""";

    private readonly IJSRuntime _jsRuntime;
    private readonly JSCallbackFactory _callbacks;
    private readonly JSCallback<CookieChangeEvent> _onChange = new();
    private Task<IJSObjectReference>? _referenceTask;
    private Task<IJSObjectReference>? _onChangeFunctionTask;
    private bool _disposed;

    internal CookieStore(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        _callbacks = new JSCallbackFactory(jsRuntime);
    }

    /// <summary>
    /// Gets the first cookie with the specified name.
    /// </summary>
    /// <param name="name">The name of the cookie to retrieve.</param>
    /// <returns>The first cookie named <paramref name="name"/>, or <see langword="null"/> if no such cookie exists.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException">The browser does not provide the Cookie Store API for the current page.</exception>
    public async ValueTask<CookieListItem?> GetAsync(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var cookieStore = await GetReferenceAsync().ConfigureAwait(false);

        return await cookieStore.InvokeAsync<CookieListItem?>("get", name).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the first cookie that matches the specified options.
    /// </summary>
    /// <param name="options">The options that select the cookie.</param>
    /// <returns>The first matching cookie, or <see langword="null"/> if no cookie matches.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException">The browser does not provide the Cookie Store API for the current page.</exception>
    [DynamicDependency(LinkerFlags.JsonSerialized, typeof(CookieStoreGetOptions))]
    public async ValueTask<CookieListItem?> GetAsync(CookieStoreGetOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var cookieStore = await GetReferenceAsync().ConfigureAwait(false);

        return await cookieStore.InvokeAsync<CookieListItem?>("get", options).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets all cookies visible to the current page.
    /// </summary>
    /// <returns>The cookies visible to the current page.</returns>
    /// <exception cref="NotSupportedException">The browser does not provide the Cookie Store API for the current page.</exception>
    [DynamicDependency(LinkerFlags.JsonSerialized, typeof(CookieListItem))]
    public async ValueTask<IReadOnlyList<CookieListItem>> GetAllAsync()
    {
        var cookieStore = await GetReferenceAsync().ConfigureAwait(false);

        return await cookieStore.InvokeAsync<CookieListItem[]>("getAll").ConfigureAwait(false);
    }

    /// <summary>
    /// Gets all cookies with the specified name.
    /// </summary>
    /// <param name="name">The name of the cookies to retrieve.</param>
    /// <returns>The cookies named <paramref name="name"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException">The browser does not provide the Cookie Store API for the current page.</exception>
    [DynamicDependency(LinkerFlags.JsonSerialized, typeof(CookieListItem))]
    public async ValueTask<IReadOnlyList<CookieListItem>> GetAllAsync(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var cookieStore = await GetReferenceAsync().ConfigureAwait(false);

        return await cookieStore.InvokeAsync<CookieListItem[]>("getAll", name).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets all cookies that match the specified options.
    /// </summary>
    /// <param name="options">The options that select the cookies.</param>
    /// <returns>The matching cookies.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException">The browser does not provide the Cookie Store API for the current page.</exception>
    [DynamicDependency(LinkerFlags.JsonSerialized, typeof(CookieStoreGetOptions))]
    [DynamicDependency(LinkerFlags.JsonSerialized, typeof(CookieListItem))]
    public async ValueTask<IReadOnlyList<CookieListItem>> GetAllAsync(CookieStoreGetOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var cookieStore = await GetReferenceAsync().ConfigureAwait(false);

        return await cookieStore.InvokeAsync<CookieListItem[]>("getAll", options).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the unpartitioned cookie with the specified name and value for the host of the current page and the path <c>"/"</c>,
    /// replacing any existing unpartitioned cookie with the same name, host, and path.
    /// </summary>
    /// <param name="name">The name of the cookie.</param>
    /// <param name="value">The value of the cookie.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException">The browser does not provide the Cookie Store API for the current page.</exception>
    public async ValueTask SetAsync(string name, string value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);

        var cookieStore = await GetReferenceAsync().ConfigureAwait(false);
        await cookieStore.InvokeVoidAsync("set", name, value).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets a cookie with the specified options, replacing any existing cookie with the same name, domain, and path that has the same partitioning.
    /// </summary>
    /// <param name="options">The name, value, and attributes of the cookie.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException">The browser does not provide the Cookie Store API for the current page.</exception>
    [DynamicDependency(LinkerFlags.JsonSerialized, typeof(CookieInit))]
    public async ValueTask SetAsync(CookieInit options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var cookieStore = await GetReferenceAsync().ConfigureAwait(false);
        await cookieStore.InvokeVoidAsync("set", options).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes the unpartitioned cookie with the specified name that is restricted to the host of the current page and has the path <c>"/"</c>, if it exists.
    /// A partitioned cookie, or a cookie with another domain or path, is not deleted.
    /// </summary>
    /// <param name="name">The name of the cookie to delete.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException">The browser does not provide the Cookie Store API for the current page.</exception>
    public async ValueTask DeleteAsync(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var cookieStore = await GetReferenceAsync().ConfigureAwait(false);
        await cookieStore.InvokeVoidAsync("delete", name).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes the cookie that matches the specified options, if it exists.
    /// </summary>
    /// <param name="options">The name and attributes of the cookie to delete.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException">The browser does not provide the Cookie Store API for the current page.</exception>
    [DynamicDependency(LinkerFlags.JsonSerialized, typeof(CookieStoreDeleteOptions))]
    public async ValueTask DeleteAsync(CookieStoreDeleteOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var cookieStore = await GetReferenceAsync().ConfigureAwait(false);
        await cookieStore.InvokeVoidAsync("delete", options).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the handler that is called when cookies visible to the current page change, replacing any previous handler.
    /// </summary>
    /// <param name="handler">The handler to call, or <see langword="null"/> to remove the current handler.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    /// <exception cref="NotSupportedException">The browser does not provide the Cookie Store API for the current page.</exception>
    [DynamicDependency(LinkerFlags.JsonSerialized, typeof(CookieListItem))]
    public async ValueTask SetOnChangeAsync(Func<CookieChangeEvent, Task>? handler)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Swap the handler before any await, so the last call wins even if calls overlap.
        _onChange.Handler = handler;

        var cookieStore = await GetReferenceAsync().ConfigureAwait(false);

        if (handler is null)
        {
            await cookieStore.SetValueAsync<IJSObjectReference?>("onchange", null).ConfigureAwait(false);
            return;
        }

        if (_onChangeFunctionTask is null || _onChangeFunctionTask.IsFaulted || _onChangeFunctionTask.IsCanceled)
        {
            _onChangeFunctionTask = _callbacks.CreateAsync(_onChange, OnChangeShapes);
        }

        var function = await _onChangeFunctionTask.ConfigureAwait(false);
        await cookieStore.SetValueAsync("onchange", function).ConfigureAwait(false);
    }

    async ValueTask IInternalAsyncDisposal.InternalDisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _onChange.Handler = null;

        // Don't wait for a pending acquisition. It could hold up scope disposal until the JS interop timeout.
        if (_referenceTask is { IsCompletedSuccessfully: true } referenceTask)
        {
            var cookieStore = referenceTask.Result;

            if (_onChangeFunctionTask is { IsCompletedSuccessfully: true })
            {
                try
                {
                    await cookieStore.SetValueAsync<IJSObjectReference?>("onchange", null).ConfigureAwait(false);
                }
                catch (JSDisconnectedException)
                {
                    // The page is already gone, so nothing can call the handler.
                }
            }

            await cookieStore.DisposeAsync().ConfigureAwait(false);
        }

        await ((IInternalAsyncDisposal)_callbacks).InternalDisposeAsync().ConfigureAwait(false);
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
        const string NotAvailableMessage = "The Cookie Store API is not available for the current page.";

        IJSObjectReference? reference;
        try
        {
            reference = await _jsRuntime.GetValueAsync<IJSObjectReference?>("cookieStore").ConfigureAwait(false);
        }
        catch (JSException ex)
        {
            // Interop throws for a missing window.cookieStore, such as on an insecure page or in an older browser.
            throw new NotSupportedException(NotAvailableMessage, ex);
        }

        return reference ?? throw new NotSupportedException(NotAvailableMessage);
    }
}
