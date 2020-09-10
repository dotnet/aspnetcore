// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;

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
        [return: MaybeNull]
        public TValue Invoke<TValue>(string identifier, params object?[]? args)
        {
            ThrowIfDisposed();

            return _jsRuntime.Invoke<TValue>(identifier, Id, args);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                base.Dispose(disposing);

                _jsRuntime.InvokeVoid("DotNet.jsCallDispatcher.disposeJSObjectReferenceById", Id);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
