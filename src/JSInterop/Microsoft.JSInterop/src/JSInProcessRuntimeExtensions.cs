// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Extensions for <see cref="IJSInProcessRuntime"/>.
    /// </summary>
    public static class JSInProcessRuntimeExtensions
    {
        /// <summary>
        /// Invokes the specified JavaScript function synchronously.
        /// </summary>
        /// <param name="jsRuntime">The <see cref="IJSInProcessRuntime"/>.</param>
        /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
        /// <param name="args">JSON-serializable arguments.</param>
        public static void InvokeVoid(this IJSInProcessRuntime jsRuntime, string identifier, params object?[] args)
        {
            if (jsRuntime == null)
            {
                throw new ArgumentNullException(nameof(jsRuntime));
            }

            jsRuntime.Invoke<object>(identifier, args);
        }
    }
}
