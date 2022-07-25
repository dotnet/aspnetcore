// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop.Implementation;

namespace Microsoft.JSInterop.WebAssembly;

internal sealed class WebAssemblyJSObjectReference : JSInProcessObjectReference, IJSUnmarshalledObjectReference
{
    private readonly WebAssemblyJSRuntime _jsRuntime;

    public WebAssemblyJSObjectReference(WebAssemblyJSRuntime jsRuntime, long id)
        : base(jsRuntime, id)
    {
        _jsRuntime = jsRuntime;
    }

#pragma warning disable CS0612 // Type or member is obsolete
    [Obsolete("This method is obsolete. Use JSImportAttribute instead.")]
    public TResult InvokeUnmarshalled<TResult>(string identifier)
    {
        ThrowIfDisposed();

        return _jsRuntime.InvokeUnmarshalled<object?, object?, object?, TResult>(identifier, null, null, null, Id);
    }

    [Obsolete("This method is obsolete. Use JSImportAttribute instead.")]
    public TResult InvokeUnmarshalled<T0, TResult>(string identifier, T0 arg0)
    {
        ThrowIfDisposed();

        return _jsRuntime.InvokeUnmarshalled<T0, object?, object?, TResult>(identifier, arg0, null, null, Id);
    }

    [Obsolete("This method is obsolete. Use JSImportAttribute instead.")]
    public TResult InvokeUnmarshalled<T0, T1, TResult>(string identifier, T0 arg0, T1 arg1)
    {
        ThrowIfDisposed();

        return _jsRuntime.InvokeUnmarshalled<T0, T1, object?, TResult>(identifier, arg0, arg1, null, Id);
    }

    [Obsolete("This method is obsolete. Use JSImportAttribute instead.")]
    public TResult InvokeUnmarshalled<T0, T1, T2, TResult>(string identifier, T0 arg0, T1 arg1, T2 arg2)
    {
        ThrowIfDisposed();

        return _jsRuntime.InvokeUnmarshalled<T0, T1, T2, TResult>(identifier, arg0, arg1, arg2, Id);
    }
#pragma warning restore CS0612 // Type or member is obsolete
}
