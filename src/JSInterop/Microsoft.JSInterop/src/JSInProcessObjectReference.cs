// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Represents a reference to a JavaScript object whose functions can be invoked synchronously.
    /// </summary>
    public class JSInProcessObjectReference : JSObjectReference, IJSInProcessRuntime
    {
        private readonly JSInProcessRuntime _jsRuntime;

        internal JSInProcessObjectReference(JSInProcessRuntime jsRuntime, long id) : base(jsRuntime, id)
        {
            _jsRuntime = jsRuntime;
        }

        /// <inheritdoc />
        [return: MaybeNull]
        public TValue Invoke<TValue>(string identifier, params object[] args)
        {
            ThrowIfDisposed();

            return _jsRuntime.Invoke<TValue>(identifier, Id, args);
        }
    }
}
