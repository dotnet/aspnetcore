// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Creates and parses browser URL objects.
/// </summary>
public sealed class UrlConstructor
{
    private readonly IJSRuntime _jsRuntime;

    internal UrlConstructor(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Creates a URL from an absolute URL.
    /// </summary>
    /// <param name="url">The absolute URL.</param>
    /// <returns>The created browser URL.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="url"/> is <see langword="null"/>.</exception>
    public ValueTask<Url> CreateAsync(string url)
    {
        ArgumentNullException.ThrowIfNull(url);

        return CreateCoreAsync([url]);
    }

    /// <summary>
    /// Creates a URL by resolving a URL against a base URL.
    /// </summary>
    /// <param name="url">The absolute or relative URL.</param>
    /// <param name="baseUrl">The base URL.</param>
    /// <returns>The created browser URL.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="url"/> or <paramref name="baseUrl"/> is <see langword="null"/>.</exception>
    public ValueTask<Url> CreateAsync(string url, string baseUrl)
    {
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(baseUrl);

        return CreateCoreAsync([url, baseUrl]);
    }

    /// <summary>
    /// Parses an absolute URL.
    /// </summary>
    /// <param name="url">The absolute URL.</param>
    /// <returns>The parsed browser URL, or <see langword="null"/> if <paramref name="url"/> cannot be parsed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="url"/> is <see langword="null"/>.</exception>
    public ValueTask<Url?> ParseAsync(string url)
    {
        ArgumentNullException.ThrowIfNull(url);

        return ParseCoreAsync([url]);
    }

    /// <summary>
    /// Parses a URL by resolving it against a base URL.
    /// </summary>
    /// <param name="url">The absolute or relative URL.</param>
    /// <param name="baseUrl">The base URL.</param>
    /// <returns>The parsed browser URL, or <see langword="null"/> if the URL cannot be parsed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="url"/> or <paramref name="baseUrl"/> is <see langword="null"/>.</exception>
    public ValueTask<Url?> ParseAsync(string url, string baseUrl)
    {
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(baseUrl);

        return ParseCoreAsync([url, baseUrl]);
    }

    /// <summary>
    /// Determines whether an absolute URL can be parsed.
    /// </summary>
    /// <param name="url">The absolute URL.</param>
    /// <returns><see langword="true"/> if <paramref name="url"/> can be parsed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="url"/> is <see langword="null"/>.</exception>
    public ValueTask<bool> CanParseAsync(string url)
    {
        ArgumentNullException.ThrowIfNull(url);

        return _jsRuntime.InvokeAsync<bool>("URL.canParse", url);
    }

    /// <summary>
    /// Determines whether a URL can be parsed relative to a base URL.
    /// </summary>
    /// <param name="url">The absolute or relative URL.</param>
    /// <param name="baseUrl">The base URL.</param>
    /// <returns><see langword="true"/> if the URL can be parsed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="url"/> or <paramref name="baseUrl"/> is <see langword="null"/>.</exception>
    public ValueTask<bool> CanParseAsync(string url, string baseUrl)
    {
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(baseUrl);

        return _jsRuntime.InvokeAsync<bool>("URL.canParse", url, baseUrl);
    }

    private async ValueTask<Url> CreateCoreAsync(object?[] arguments)
    {
        var reference = await _jsRuntime.InvokeConstructorAsync("URL", arguments).ConfigureAwait(false);

        return new Url(reference);
    }

    private async ValueTask<Url?> ParseCoreAsync(object?[] arguments)
    {
        var reference = await _jsRuntime.InvokeAsync<IJSObjectReference?>("URL.parse", arguments).ConfigureAwait(false);

        return reference is null ? null : new Url(reference);
    }
}
