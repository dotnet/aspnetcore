// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents the state and identity of the browser.
/// </summary>
public sealed class Navigator : IInternalAsyncDisposal
{
    private readonly IJSRuntime _jsRuntime;
    private readonly Window _window;
    private readonly FeatureContext _context;
    private Task<IJSObjectReference>? _referenceTask;
    private bool _disposed;

    internal Navigator(IJSRuntime jsRuntime, Window window)
    {
        _jsRuntime = jsRuntime;
        _window = window;
        // Window will own the context once events need parts shared by the whole scope.
        _context = new FeatureContext(jsRuntime);
    }

    /// <summary>
    /// Gets the preferred language of the user, usually the language of the browser UI.
    /// </summary>
    /// <returns>The language tag, such as <c>"en-US"</c>.</returns>
    public async ValueTask<string> GetLanguageAsync()
    {
        var navigator = await GetReferenceAsync().ConfigureAwait(false);

        return await navigator.GetValueAsync<string>("language").ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the languages that the user prefers, with the most preferred language first.
    /// </summary>
    /// <returns>The language tags, such as <c>"en-US"</c>.</returns>
    public async ValueTask<IReadOnlyList<string>> GetLanguagesAsync()
    {
        var navigator = await GetReferenceAsync().ConfigureAwait(false);

        return await navigator.GetValueAsync<string[]>("languages").ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a value indicating whether the browser is online.
    /// </summary>
    /// <remarks>
    /// A value of <see langword="true"/> does not mean that the internet can be reached.
    /// </remarks>
    /// <returns><see langword="true"/> if the browser is online; otherwise, <see langword="false"/>.</returns>
    public async ValueTask<bool> GetOnLineAsync()
    {
        var navigator = await GetReferenceAsync().ConfigureAwait(false);

        return await navigator.GetValueAsync<bool>("onLine").ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a value indicating whether the browser accepts cookies.
    /// </summary>
    /// <returns><see langword="true"/> if the browser accepts cookies; otherwise, <see langword="false"/>.</returns>
    public async ValueTask<bool> GetCookieEnabledAsync()
    {
        var navigator = await GetReferenceAsync().ConfigureAwait(false);

        return await navigator.GetValueAsync<bool>("cookieEnabled").ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the user agent string of the browser.
    /// </summary>
    /// <returns>The user agent string.</returns>
    public async ValueTask<string> GetUserAgentAsync()
    {
        var navigator = await GetReferenceAsync().ConfigureAwait(false);

        return await navigator.GetValueAsync<string>("userAgent").ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the number of logical processors that the browser can use to run threads.
    /// </summary>
    /// <returns>The number of logical processors.</returns>
    public async ValueTask<ulong> GetHardwareConcurrencyAsync()
    {
        var navigator = await GetReferenceAsync().ConfigureAwait(false);

        return await navigator.GetValueAsync<ulong>("hardwareConcurrency").ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the maximum number of simultaneous touch points that the device supports.
    /// </summary>
    /// <returns>The maximum number of touch points, or 0 if the device does not support touch input.</returns>
    public async ValueTask<int> GetMaxTouchPointsAsync()
    {
        var navigator = await GetReferenceAsync().ConfigureAwait(false);

        return await navigator.GetValueAsync<int>("maxTouchPoints").ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the projection of type <typeparamref name="T"/> that hangs off the navigator, creating it on first access.
    /// </summary>
    /// <typeparam name="T">The projection type.</typeparam>
    /// <param name="factory">The callback that creates the projection.</param>
    /// <returns>The projection keyed by <typeparamref name="T"/>.</returns>
    internal T GetOrAddFeature<T>(Func<FeatureContext, T> factory) where T : class
    {
        ArgumentNullException.ThrowIfNull(factory);

        // Window keeps every projection of the scope, so its disposal reaches this one too.
        return _window.GetOrAddFeature(_ => factory(_context));
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
            _referenceTask = _jsRuntime.GetValueAsync<IJSObjectReference>("navigator").AsTask();
        }

        return _referenceTask;
    }
}
