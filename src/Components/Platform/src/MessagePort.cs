// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents one endpoint of a browser message channel.
/// </summary>
public sealed class MessagePort : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IJSObjectReference _ownerReference;
    private readonly string _propertyName;
    private Task<IJSObjectReference>? _referenceTask;
    private bool _disposed;

    internal MessagePort(IJSRuntime jsRuntime, IJSObjectReference ownerReference, string propertyName)
    {
        _jsRuntime = jsRuntime;
        _ownerReference = ownerReference;
        _propertyName = propertyName;
    }

    /// <summary>
    /// Posts a literal string to the connected port.
    /// </summary>
    /// <param name="message">The string to post.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of posting, not delivery.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is <see langword="null"/>.</exception>
    public async ValueTask PostMessageAsync(string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var reference = await GetReferenceAsync().ConfigureAwait(false);
        await reference.InvokeVoidAsync("postMessage", message).ConfigureAwait(false);
    }

    /// <summary>
    /// Posts the JSON representation of a value to the connected port.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="message">The value to serialize and post.</param>
    /// <param name="jsonTypeInfo">The JSON serialization metadata for the value.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of posting, not delivery.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="jsonTypeInfo"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The supplied contract determines the JSON representation. CLR type identity and non-JSON structured-clone values are not preserved.
    /// JSON numbers are converted to JavaScript numbers, which can lose large-integer or decimal precision.
    /// </remarks>
    public ValueTask PostMessageAsync<T>(T message, JsonTypeInfo<T> jsonTypeInfo)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(jsonTypeInfo);

        return PostMessageAsync(JsonSerializer.SerializeToElement(message, jsonTypeInfo));
    }

    /// <summary>
    /// Posts a JSON-compatible value to the connected port.
    /// </summary>
    /// <param name="message">The JSON value to post.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of posting, not delivery.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="message"/> has an undefined value kind.</exception>
    /// <exception cref="ObjectDisposedException">This port or the document containing <paramref name="message"/> has been disposed.</exception>
    /// <remarks>Only JSON values are supported. Transferable objects and other structured-clone values are not supported.</remarks>
    public async ValueTask PostMessageAsync(JsonElement message)
    {
        var reference = await GetReferenceAsync().ConfigureAwait(false);
        await BrowserMessaging.PostMessageAsync(_jsRuntime, reference, message).ConfigureAwait(false);
    }

    /// <summary>
    /// Enables dispatch of queued messages on this port.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    public async ValueTask StartAsync()
    {
        var reference = await GetReferenceAsync().ConfigureAwait(false);
        await reference.InvokeVoidAsync("start").ConfigureAwait(false);
    }

    /// <summary>
    /// Disconnects this port from its connected port.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    public async ValueTask CloseAsync()
    {
        var reference = await GetReferenceAsync().ConfigureAwait(false);
        await reference.InvokeVoidAsync("close").ConfigureAwait(false);
    }

    /// <summary>
    /// Closes this port if it has been used and releases its resources.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    /// <remarks>
    /// Ports obtained from a <see cref="MessageChannel"/> are owned by that channel and should not be disposed separately.
    /// Disposal does not wait for an operation that is still obtaining the port.
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
            try
            {
                await referenceTask.Result.InvokeVoidAsync("close").ConfigureAwait(false);
            }
            finally
            {
                await referenceTask.Result.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private Task<IJSObjectReference> GetReferenceAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_referenceTask is null || _referenceTask.IsFaulted || _referenceTask.IsCanceled)
        {
            _referenceTask = _ownerReference.GetValueAsync<IJSObjectReference>(_propertyName).AsTask();
        }

        return _referenceTask;
    }
}
