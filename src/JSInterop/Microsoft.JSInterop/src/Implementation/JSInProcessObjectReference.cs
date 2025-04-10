// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.JSInterop.Infrastructure;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.JSInterop.Implementation;

/// <summary>
/// Implements functionality for <see cref="IJSInProcessObjectReference"/>.
/// </summary>
public partial class JSInProcessObjectReference : JSObjectReference, IJSInProcessObjectReference
{
    private readonly JSInProcessRuntime _jsRuntime;

    /// <summary>
    /// Initializes a new <see cref="JSInProcessObjectReference"/> instance.
    /// </summary>
    /// <param name="jsRuntime">The <see cref="JSInProcessRuntime"/> used for invoking JS interop calls.</param>
    /// <param name="id">The unique identifier.</param>
    protected internal JSInProcessObjectReference(JSInProcessRuntime jsRuntime, long id) : base(jsRuntime, id)
    {
        _jsRuntime = jsRuntime;
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public TValue Invoke<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string identifier, params object?[]? args)
    {
        ThrowIfDisposed();

        return _jsRuntime.Invoke<TValue>(identifier, Id, JSCallType.FunctionCall, args);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public IJSInProcessObjectReference InvokeNew(string identifier, object?[]? args)
    {
        ThrowIfDisposed();

        return _jsRuntime.Invoke<IJSInProcessObjectReference>(identifier, Id, JSCallType.NewCall, args);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public TValue GetValue<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string identifier)
    {
        ThrowIfDisposed();

        return _jsRuntime.Invoke<TValue>(identifier, Id, JSCallType.GetValue);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    public void SetValue<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string identifier, TValue value)
    {
        ThrowIfDisposed();

        _jsRuntime.Invoke<TValue>(identifier, Id, JSCallType.SetValue, value);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!Disposed)
        {
            Disposed = true;

            DisposeJSObjectReferenceById(Id);
        }
    }

    [JSImport("globalThis.DotNet.disposeJSObjectReferenceById")]
    private static partial void DisposeJSObjectReferenceById([JSMarshalAs<JSType.Number>] long id);
}
