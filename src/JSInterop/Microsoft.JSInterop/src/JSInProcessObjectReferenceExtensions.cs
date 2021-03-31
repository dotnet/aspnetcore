// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
