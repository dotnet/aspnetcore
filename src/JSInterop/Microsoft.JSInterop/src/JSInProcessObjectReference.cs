// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.JSInterop
{
    public class JSInProcessObjectReference : JSObjectReference, IJSInProcessRuntime
    {
        private readonly JSInProcessRuntime _jsRuntime;

        internal JSInProcessObjectReference(JSInProcessRuntime jsRuntime, long id) : base(jsRuntime, id)
        {
            _jsRuntime = jsRuntime;
        }

        [return: MaybeNull]
        public TValue Invoke<TValue>(string identifier, params object[] args)
        {
            ThrowIfDisposed();

            return _jsRuntime.Invoke<TValue>(identifier, Id, args);
        }
    }
}
