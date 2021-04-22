// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Extension methods for <see cref="IJSInProcessObjectReference"/>.
    /// </summary>
    public static class JSInProcessObjectReferenceExtensions
    {
        /// <summary>
        /// Invokes the specified JavaScript function synchronously.
        /// </summary>
        /// <param name="jsObjectReference">The <see cref="IJSInProcessObjectReference"/>.</param>
        /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>someScope.someFunction</c> on the target instance.</param>
        /// <param name="args">JSON-serializable arguments.</param>
        public static void InvokeVoid(this IJSInProcessObjectReference jsObjectReference, string identifier, params object?[] args)
        {
            if (jsObjectReference == null)
            {
                throw new ArgumentNullException(nameof(jsObjectReference));
            }

            jsObjectReference.Invoke<object>(identifier, args);
        }
    }
}
