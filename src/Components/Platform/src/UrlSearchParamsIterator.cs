// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents a browser URLSearchParams iterator.
/// </summary>
/// <typeparam name="T">The type of value returned by the iterator.</typeparam>
public sealed class UrlSearchParamsIterator<T> : IAsyncDisposable
{
    private readonly IJSObjectReference _reference;
    private readonly Func<JsonElement, T> _convertValue;
    private bool _disposed;

    private UrlSearchParamsIterator(IJSObjectReference reference, Func<JsonElement, T> convertValue)
    {
        _reference = reference;
        _convertValue = convertValue;
    }

    /// <summary>
    /// Advances the iterator and gets the result.
    /// </summary>
    /// <returns>The result of advancing the iterator.</returns>
    public async ValueTask<UrlSearchParamsIteratorResult<T>> NextAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var result = await _reference.InvokeAsync<IteratorResult>("next").ConfigureAwait(false);

        return result.Done
            ? new UrlSearchParamsIteratorResult<T>(done: true, default!)
            : new UrlSearchParamsIteratorResult<T>(done: false, _convertValue(result.Value));
    }

    /// <summary>
    /// Releases the browser iterator.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await _reference.DisposeAsync().ConfigureAwait(false);
    }

    internal static UrlSearchParamsIterator<T> CreateEntries(IJSObjectReference reference)
    {
        return new(reference, static value =>
        {
            var enumerator = value.EnumerateArray();
            enumerator.MoveNext();
            var key = enumerator.Current.GetString()!;
            enumerator.MoveNext();

            return (T)(object)new KeyValuePair<string, string>(key, enumerator.Current.GetString()!);
        });
    }

    internal static UrlSearchParamsIterator<T> CreateStrings(IJSObjectReference reference)
        => new(reference, static value => (T)(object)value.GetString()!);

    private sealed class IteratorResult
    {
        public bool Done { get; init; }

        public JsonElement Value { get; init; }
    }
}

/// <summary>
/// Represents the result of advancing a browser URLSearchParams iterator.
/// </summary>
/// <typeparam name="T">The type of value returned by the iterator.</typeparam>
public readonly struct UrlSearchParamsIteratorResult<T>
{
    internal UrlSearchParamsIteratorResult(bool done, T value)
    {
        Done = done;
        Value = value;
    }

    /// <summary>
    /// Gets a value indicating whether the iterator has completed.
    /// </summary>
    public bool Done { get; }

    /// <summary>
    /// Gets the current value. The value is undefined if <see cref="Done"/> is <see langword="true"/>.
    /// </summary>
    public T Value { get; }
}
