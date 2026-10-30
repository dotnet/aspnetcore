// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents a live browser URL query parameter collection.
/// </summary>
/// <remarks>
/// Dispose instances created by <see cref="UrlSearchParamsConstructor"/>. Instances obtained from <see cref="Url.SearchParams"/> are owned by their URL and should not be disposed separately.
/// </remarks>
public sealed class UrlSearchParams : IAsyncDisposable, IInternalAsyncDisposal
{
    private Task<IJSObjectReference>? _referenceTask;
    private readonly IJSObjectReference? _ownerReference;
    private readonly string? _propertyName;
    private bool _disposed;

    internal UrlSearchParams(IJSObjectReference reference)
    {
        _referenceTask = Task.FromResult(reference);
    }

    internal UrlSearchParams(IJSObjectReference ownerReference, string propertyName)
    {
        _ownerReference = ownerReference;
        _propertyName = propertyName;
    }

    /// <summary>
    /// Gets the number of query parameters.
    /// </summary>
    /// <returns>The number of query parameters.</returns>
    public async ValueTask<uint> GetSizeAsync()
    {
        var reference = await GetReferenceAsync().ConfigureAwait(false);

        return await reference.GetValueAsync<uint>("size").ConfigureAwait(false);
    }

    /// <summary>
    /// Appends a query parameter.
    /// </summary>
    /// <param name="name">The query parameter name.</param>
    /// <param name="value">The query parameter value.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="value"/> is <see langword="null"/>.</exception>
    public async ValueTask AppendAsync(string name, string value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);

        var reference = await GetReferenceAsync().ConfigureAwait(false);
        await reference.InvokeVoidAsync("append", name, value).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes all query parameters with the specified name.
    /// </summary>
    /// <param name="name">The query parameter name.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    public async ValueTask DeleteAsync(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var reference = await GetReferenceAsync().ConfigureAwait(false);
        await reference.InvokeVoidAsync("delete", name).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes query parameters with the specified name and value.
    /// </summary>
    /// <param name="name">The query parameter name.</param>
    /// <param name="value">The query parameter value.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="value"/> is <see langword="null"/>.</exception>
    public async ValueTask DeleteAsync(string name, string value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);

        var reference = await GetReferenceAsync().ConfigureAwait(false);
        await reference.InvokeVoidAsync("delete", name, value).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the first value associated with a query parameter.
    /// </summary>
    /// <param name="name">The query parameter name.</param>
    /// <returns>The first value associated with <paramref name="name"/>, or <see langword="null"/> if the query parameter does not exist.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    public async ValueTask<string?> GetAsync(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var reference = await GetReferenceAsync().ConfigureAwait(false);

        return await reference.InvokeAsync<string?>("get", name).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets all values associated with a query parameter.
    /// </summary>
    /// <param name="name">The query parameter name.</param>
    /// <returns>All values associated with <paramref name="name"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    public async ValueTask<IReadOnlyList<string>> GetAllAsync(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var reference = await GetReferenceAsync().ConfigureAwait(false);

        return await reference.InvokeAsync<string[]>("getAll", name).ConfigureAwait(false);
    }

    /// <summary>
    /// Determines whether a query parameter with the specified name exists.
    /// </summary>
    /// <param name="name">The query parameter name.</param>
    /// <returns><see langword="true"/> if the query parameter exists; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    public async ValueTask<bool> HasAsync(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var reference = await GetReferenceAsync().ConfigureAwait(false);

        return await reference.InvokeAsync<bool>("has", name).ConfigureAwait(false);
    }

    /// <summary>
    /// Determines whether a query parameter with the specified name and value exists.
    /// </summary>
    /// <param name="name">The query parameter name.</param>
    /// <param name="value">The query parameter value.</param>
    /// <returns><see langword="true"/> if the query parameter exists; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="value"/> is <see langword="null"/>.</exception>
    public async ValueTask<bool> HasAsync(string name, string value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);

        var reference = await GetReferenceAsync().ConfigureAwait(false);

        return await reference.InvokeAsync<bool>("has", name, value).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets a query parameter, replacing all existing values with the specified value.
    /// </summary>
    /// <param name="name">The query parameter name.</param>
    /// <param name="value">The query parameter value.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="value"/> is <see langword="null"/>.</exception>
    public async ValueTask SetAsync(string name, string value)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);

        var reference = await GetReferenceAsync().ConfigureAwait(false);
        await reference.InvokeVoidAsync("set", name, value).ConfigureAwait(false);
    }

    /// <summary>
    /// Sorts query parameters by name.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    public async ValueTask SortAsync()
    {
        var reference = await GetReferenceAsync().ConfigureAwait(false);
        await reference.InvokeVoidAsync("sort").ConfigureAwait(false);
    }

    /// <summary>
    /// Enumerates the live query parameter names and values.
    /// </summary>
    /// <returns>An asynchronous sequence of query parameter names and values.</returns>
    /// <remarks>
    /// Each enumeration starts a new live iterator on its first advance. Keep the collection alive
    /// until enumeration ends. Cancellation is observed between browser operations; it does not abort an operation in progress.
    /// </remarks>
    public IAsyncEnumerable<KeyValuePair<string, string>> EntriesAsync()
        => EnumerateAsync("entries", static value => new KeyValuePair<string, string>(value[0].GetString()!, value[1].GetString()!));

    /// <summary>
    /// Enumerates the live query parameter names.
    /// </summary>
    /// <returns>An asynchronous sequence of query parameter names.</returns>
    /// <remarks>
    /// Each enumeration starts a new live iterator on its first advance. Keep the collection alive
    /// until enumeration ends. Cancellation is observed between browser operations; it does not abort an operation in progress.
    /// </remarks>
    public IAsyncEnumerable<string> KeysAsync()
        => EnumerateAsync("keys", static value => value.GetString()!);

    /// <summary>
    /// Enumerates the live query parameter values.
    /// </summary>
    /// <returns>An asynchronous sequence of query parameter values.</returns>
    /// <remarks>
    /// Each enumeration starts a new live iterator on its first advance. Keep the collection alive
    /// until enumeration ends. Cancellation is observed between browser operations; it does not abort an operation in progress.
    /// </remarks>
    public IAsyncEnumerable<string> ValuesAsync()
        => EnumerateAsync("values", static value => value.GetString()!);

    /// <summary>
    /// Converts the query parameters to their serialized string form.
    /// </summary>
    /// <returns>The serialized query parameters.</returns>
    public async ValueTask<string> ToStringAsync()
    {
        var reference = await GetReferenceAsync().ConfigureAwait(false);

        return await reference.InvokeAsync<string>("toString").ConfigureAwait(false);
    }

    /// <summary>
    /// Releases the query parameter collection.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    /// <remarks>
    /// Do not dispose this instance if it was obtained from <see cref="Url.SearchParams"/>. The containing URL owns that instance.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_referenceTask is { IsCompletedSuccessfully: true } referenceTask)
        {
            await referenceTask.Result.DisposeAsync().ConfigureAwait(false);
        }
    }

    ValueTask IInternalAsyncDisposal.InternalDisposeAsync()
        => DisposeAsync();

    private async IAsyncEnumerable<T> EnumerateAsync<T>(
        string identifier,
        Func<JsonElement, T> convertValue,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var reference = await GetReferenceAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var iterator = await reference.InvokeAsync<IJSObjectReference>(identifier).ConfigureAwait(false);
        await using (iterator.ConfigureAwait(false))
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ObjectDisposedException.ThrowIf(_disposed, this);
                var result = await iterator.InvokeAsync<IteratorResult>("next").ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                ObjectDisposedException.ThrowIf(_disposed, this);

                if (result.Done)
                {
                    yield break;
                }

                yield return convertValue(result.Value);
            }
        }
    }

    private sealed class IteratorResult
    {
        public bool Done { get; init; }

        public JsonElement Value { get; init; }
    }

    private Task<IJSObjectReference> GetReferenceAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_referenceTask is null || _referenceTask.IsFaulted || _referenceTask.IsCanceled)
        {
            _referenceTask = _ownerReference!.GetValueAsync<IJSObjectReference>(_propertyName!).AsTask();
        }

        return _referenceTask;
    }
}
