// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.JSInterop.Implementation;

namespace Microsoft.JSInterop.WebAssembly
{
    internal class WebAssemblyJSObjectReference : JSInProcessObjectReference, IJSUnmarshalledObjectReference
    {
        private readonly WebAssemblyJSRuntime _jsRuntime;

        public WebAssemblyJSObjectReference(WebAssemblyJSRuntime jsRuntime, long id)
            : base(jsRuntime, id)
        {
            _jsRuntime = jsRuntime;
        }

        internal new long Id => base.Id;

        public TResult InvokeUnmarshalled<TResult>(string identifier)
        {
            ThrowIfDisposed();

            return _jsRuntime.InvokeUnmarshalled<object?, object?, object?, TResult>(identifier, null, null, null, Id);
        }

        public TResult InvokeUnmarshalled<T0, TResult>(string identifier, T0 arg0)
        {
            ThrowIfDisposed();

            return _jsRuntime.InvokeUnmarshalled<T0, object?, object?, TResult>(identifier, arg0, null, null, Id);
        }

        public TResult InvokeUnmarshalled<T0, T1, TResult>(string identifier, T0 arg0, T1 arg1)
        {
            ThrowIfDisposed();

            return _jsRuntime.InvokeUnmarshalled<T0, T1, object?, TResult>(identifier, arg0, arg1, null, Id);
        }

        public TResult InvokeUnmarshalled<T0, T1, T2, TResult>(string identifier, T0 arg0, T1 arg1, T2 arg2)
        {
            ThrowIfDisposed();

            return _jsRuntime.InvokeUnmarshalled<T0, T1, T2, TResult>(identifier, arg0, arg1, arg2, Id);
        }
    }
}
