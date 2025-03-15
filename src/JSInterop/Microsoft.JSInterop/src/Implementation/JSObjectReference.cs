// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop.Infrastructure;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.JSInterop.Implementation;

/// <summary>
/// Implements functionality for <see cref="IJSObjectReference"/>.
/// </summary>
public class JSObjectReference : IJSObjectReference
{
    private readonly JSRuntime _jsRuntime;

    internal bool Disposed { get; set; }

    /// <summary>
    /// The unique identifier assigned to this instance.
    /// </summary>
    protected internal long Id { get; }

    /// <summary>
    /// Initializes a new <see cref="JSObjectReference"/> instance.
    /// </summary>
    /// <param name="jsRuntime">The <see cref="JSRuntime"/> used for invoking JS interop calls.</param>
    /// <param name="id">The unique identifier.</param>
    protected internal JSObjectReference(JSRuntime jsRuntime, long id)
    {
        _jsRuntime = jsRuntime;

        Id = id;
    }

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string identifier, object?[]? args)
    {
        ThrowIfDisposed();

        return _jsRuntime.InvokeAsync<TValue>(Id, identifier, args);
    }

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        ThrowIfDisposed();

        return _jsRuntime.InvokeAsync<TValue>(Id, identifier, cancellationToken, args);
    }

    /// <inheritdoc />
    public ValueTask<IJSObjectReference> InvokeNewAsync(string identifier, object?[]? args)
        => InvokeAsync<IJSObjectReference>($"new:{identifier}", args);

    /// <inheritdoc />
    public ValueTask<IJSObjectReference> InvokeNewAsync(string identifier, CancellationToken cancellationToken, object?[]? args)
        => InvokeAsync<IJSObjectReference>($"new:{identifier}", cancellationToken, args);

    /// <inheritdoc />
    public ValueTask<TValue> GetValueAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>()
        => InvokeAsync<TValue>("get:", null);

    /// <inheritdoc />
    public ValueTask<TValue> GetValueAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(CancellationToken cancellationToken)
        => InvokeAsync<TValue>("get:", cancellationToken, null);

    /// <inheritdoc />
    public ValueTask<TValue> GetValueAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string identifier)
        => InvokeAsync<TValue>($"get:{identifier}", null);

    /// <inheritdoc />
    public ValueTask<TValue> GetValueAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string identifier, CancellationToken cancellationToken)
        => InvokeAsync<TValue>($"get:{identifier}", cancellationToken, null);

    /// <inheritdoc />
    public async ValueTask SetValueAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string identifier, TValue value)
        => await InvokeAsync<IJSVoidResult>($"set:{identifier}", [value]);

    /// <inheritdoc />
    public async ValueTask SetValueAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string identifier, TValue value, CancellationToken cancellationToken)
        => await InvokeAsync<IJSVoidResult>($"set:{identifier}", cancellationToken, [value]);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!Disposed)
        {
            Disposed = true;

            await _jsRuntime.InvokeVoidAsync("DotNet.disposeJSObjectReferenceById", Id);
        }
    }

    /// <inheritdoc />
    protected void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(Disposed, this);
    }
}
