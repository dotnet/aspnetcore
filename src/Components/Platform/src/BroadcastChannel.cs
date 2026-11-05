// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents a named browser broadcast channel.
/// </summary>
public sealed class BroadcastChannel : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IJSObjectReference _reference;
    private bool _disposed;

    internal BroadcastChannel(IJSRuntime jsRuntime, IJSObjectReference reference)
    {
        _jsRuntime = jsRuntime;
        _reference = reference;
    }

    /// <summary>
    /// Gets the channel name.
    /// </summary>
    /// <returns>The channel name.</returns>
    public ValueTask<string> GetNameAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _reference.GetValueAsync<string>("name");
    }

    /// <summary>
    /// Posts a literal string to other eligible broadcast channels with the same name.
    /// </summary>
    /// <param name="message">The string to post.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of posting, not delivery.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is <see langword="null"/>.</exception>
    public ValueTask PostMessageAsync(string message)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(message);

        return _reference.InvokeVoidAsync("postMessage", message);
    }

    /// <summary>
    /// Posts the JSON representation of a value to other eligible broadcast channels with the same name.
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
    /// Posts a JSON-compatible value to other eligible broadcast channels with the same name.
    /// </summary>
    /// <param name="message">The JSON value to post.</param>
    /// <returns>A <see cref="ValueTask"/> representing the completion of posting, not delivery.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="message"/> has an undefined value kind.</exception>
    /// <exception cref="ObjectDisposedException">This channel or the document containing <paramref name="message"/> has been disposed.</exception>
    /// <remarks>Only JSON values are supported. Transferable objects and other structured-clone values are not supported.</remarks>
    public ValueTask PostMessageAsync(JsonElement message)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return BrowserMessaging.PostMessageAsync(_jsRuntime, _reference, message);
    }

    /// <summary>
    /// Closes this channel.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    public ValueTask CloseAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _reference.InvokeVoidAsync("close");
    }

    /// <summary>
    /// Closes this channel and releases its resources.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            await _reference.InvokeVoidAsync("close").ConfigureAwait(false);
        }
        finally
        {
            await _reference.DisposeAsync().ConfigureAwait(false);
        }
    }
}
