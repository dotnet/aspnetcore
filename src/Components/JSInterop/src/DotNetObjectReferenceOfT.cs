// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop.Infrastructure;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.JSInterop;

/// <summary>
/// Wraps a JS interop argument, indicating that the value should not be serialized as JSON
/// but instead should be passed as a reference.
///
/// To avoid leaking memory, the reference must later be disposed by JS code or by .NET code.
/// </summary>
/// <typeparam name="TValue">The type of the value to wrap.</typeparam>
public sealed class DotNetObjectReference<[DynamicallyAccessedMembers(JSInvokable)] TValue> :
    IDotNetObjectReference, IDisposable where TValue : class
{
    private readonly TValue _value;
    private long _objectId;
    private JSRuntime? _jsRuntime;

    /// <summary>
    /// Initializes a new instance of <see cref="DotNetObjectReference{TValue}" />.
    /// </summary>
    /// <param name="value">The value to pass by reference.</param>
    internal DotNetObjectReference(TValue value)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the object instance represented by this wrapper.
    /// </summary>
    public TValue Value
    {
        get
        {
            ThrowIfDisposed();
            return _value;
        }
    }

    internal long ObjectId
    {
        get
        {
            ThrowIfDisposed();
            Debug.Assert(_objectId != 0, "Accessing ObjectId without tracking is always incorrect.");

            return _objectId;
        }
        set
        {
            ThrowIfDisposed();
            _objectId = value;
        }
    }

    internal JSRuntime? JSRuntime
    {
        get
        {
            ThrowIfDisposed();
            return _jsRuntime;
        }
        set
        {
            ThrowIfDisposed();
            _jsRuntime = value;
        }
    }

    object IDotNetObjectReference.Value => Value;

    internal bool Disposed { get; private set; }

    /// <summary>
    /// Stops tracking this object reference, allowing it to be garbage collected
    /// (if there are no other references to it). Once the instance is disposed, it
    /// can no longer be used in interop calls from JavaScript code.
    /// </summary>
    public void Dispose()
    {
        if (!Disposed)
        {
            Disposed = true;

            _jsRuntime?.ReleaseObjectReference(_objectId);
        }
    }

    internal void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(Disposed, this);
    }
}
