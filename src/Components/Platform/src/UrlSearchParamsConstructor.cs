// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Creates browser URLSearchParams objects.
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
    /// <returns>The owner of the created query parameter collection.</returns>
    public ValueTask<UrlSearchParamsOwner> CreateAsync()
        => CreateCoreAsync([]);

    /// <summary>
    /// Creates a query parameter collection by parsing a query string.
    /// </summary>
    /// <param name="init">The query string.</param>
    /// <returns>The owner of the created query parameter collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="init"/> is <see langword="null"/>.</exception>
    public ValueTask<UrlSearchParamsOwner> CreateAsync(string init)
    {
        ArgumentNullException.ThrowIfNull(init);

        return CreateCoreAsync([init]);
    }

    /// <summary>
    /// Creates a query parameter collection from a record of names and values.
    /// </summary>
    /// <param name="init">The query parameter names and values.</param>
    /// <returns>The owner of the created query parameter collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="init"/> is <see langword="null"/>.</exception>
    public ValueTask<UrlSearchParamsOwner> CreateAsync(IReadOnlyDictionary<string, string> init)
    {
        ArgumentNullException.ThrowIfNull(init);

        return CreateCoreAsync([init]);
    }

    /// <summary>
    /// Creates a query parameter collection from an ordered sequence of names and values.
    /// </summary>
    /// <param name="init">The ordered query parameter names and values.</param>
    /// <returns>The owner of the created query parameter collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="init"/> is <see langword="null"/>.</exception>
    public ValueTask<UrlSearchParamsOwner> CreateAsync(IEnumerable<KeyValuePair<string, string>> init)
    {
        ArgumentNullException.ThrowIfNull(init);

        var entries = init.Select(static pair => new[] { pair.Key, pair.Value }).ToArray();

        return CreateCoreAsync([entries]);
    }

    private async ValueTask<UrlSearchParamsOwner> CreateCoreAsync(object?[] arguments)
    {
        var reference = await _jsRuntime.InvokeConstructorAsync("URLSearchParams", arguments).ConfigureAwait(false);

        return new UrlSearchParamsOwner(new UrlSearchParams(reference));
    }
}
