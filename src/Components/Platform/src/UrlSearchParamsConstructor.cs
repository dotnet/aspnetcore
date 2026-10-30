// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Creates caller-owned browser URLSearchParams objects.
/// </summary>
public sealed class UrlSearchParamsConstructor
{
    private readonly IJSRuntime _jsRuntime;

    internal UrlSearchParamsConstructor(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Creates an empty query parameter collection.
    /// </summary>
    /// <returns>The created query parameter collection, which the caller must dispose.</returns>
    public ValueTask<UrlSearchParams> CreateAsync()
        => CreateCoreAsync([]);

    /// <summary>
    /// Creates a query parameter collection by parsing a query string.
    /// </summary>
    /// <param name="init">The query string.</param>
    /// <returns>The created query parameter collection, which the caller must dispose.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="init"/> is <see langword="null"/>.</exception>
    public ValueTask<UrlSearchParams> CreateAsync(string init)
    {
        ArgumentNullException.ThrowIfNull(init);

        return CreateCoreAsync([init]);
    }

    /// <summary>
    /// Creates a query parameter collection from a record of names and values.
    /// </summary>
    /// <param name="init">The query parameter names and values.</param>
    /// <returns>The created query parameter collection, which the caller must dispose.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="init"/> is <see langword="null"/>.</exception>
    public ValueTask<UrlSearchParams> CreateAsync(IReadOnlyDictionary<string, string> init)
    {
        ArgumentNullException.ThrowIfNull(init);

        return CreateCoreAsync([init]);
    }

    /// <summary>
    /// Creates a query parameter collection from an ordered sequence of names and values.
    /// </summary>
    /// <param name="init">The ordered query parameter names and values.</param>
    /// <returns>The created query parameter collection, which the caller must dispose.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="init"/> is <see langword="null"/>.</exception>
    public ValueTask<UrlSearchParams> CreateAsync(IEnumerable<KeyValuePair<string, string>> init)
    {
        ArgumentNullException.ThrowIfNull(init);

        var entries = init.Select(static pair => new[] { pair.Key, pair.Value }).ToArray();

        return CreateCoreAsync([entries]);
    }

    private async ValueTask<UrlSearchParams> CreateCoreAsync(object?[] arguments)
    {
        var reference = await _jsRuntime.InvokeConstructorAsync("URLSearchParams", arguments).ConfigureAwait(false);

        return new UrlSearchParams(reference);
    }
}
