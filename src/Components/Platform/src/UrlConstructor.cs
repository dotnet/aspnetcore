// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Creates browser URL objects.
/// </summary>
public sealed class UrlConstructor
{
    private readonly IJSRuntime _jsRuntime;

    internal UrlConstructor(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Creates a URL from an absolute URL string or resolves it against a base URL.
    /// </summary>
    /// <param name="url">The absolute or relative URL.</param>
    /// <param name="baseUrl">The optional base URL.</param>
    /// <returns>The created browser URL.</returns>
    public async ValueTask<Url> CreateAsync(
        string url,
        string? baseUrl = null)
    {
        ArgumentNullException.ThrowIfNull(url);

        object?[] arguments = baseUrl is null ? [url] : [url, baseUrl];
        var reference = await _jsRuntime
            .InvokeConstructorAsync("URL", CancellationToken.None, arguments)
            .ConfigureAwait(false);

        return new Url(reference);
    }
}
