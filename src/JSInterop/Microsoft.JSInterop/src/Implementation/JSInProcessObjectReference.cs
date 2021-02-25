// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.JSInterop.Implementation
{
    /// <summary>
    /// Implements functionality for <see cref="IJSInProcessObjectReference"/>.
    /// </summary>
    public class JSInProcessObjectReference : JSObjectReference, IJSInProcessObjectReference
    {
        private readonly JSInProcessRuntime _jsRuntime;

        /// <summary>
        /// Inititializes a new <see cref="JSInProcessObjectReference"/> instance.
        /// </summary>
        /// <param name="jsRuntime">The <see cref="JSInProcessRuntime"/> used for invoking JS interop calls.</param>
        /// <param name="id">The unique identifier.</param>
        protected internal JSInProcessObjectReference(JSInProcessRuntime jsRuntime, long id) : base(jsRuntime, id)
        {
            _jsRuntime = jsRuntime;
        }

        /// <inheritdoc />
        public TValue Invoke<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string identifier, params object?[]? args)
        {
            ThrowIfDisposed();

            return _jsRuntime.Invoke<TValue>(identifier, Id, args);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!Disposed)
            {
                Disposed = true;

                _jsRuntime.InvokeVoid("DotNet.jsCallDispatcher.disposeJSObjectReferenceById", Id);
            }
        }
    }
}
