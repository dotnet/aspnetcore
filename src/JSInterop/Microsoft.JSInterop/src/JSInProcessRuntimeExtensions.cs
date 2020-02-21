// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        public static void InvokeVoid(this IJSInProcessRuntime jsRuntime, string identifier, params object[] args)
        {
            if (jsRuntime == null)
            {
                throw new ArgumentNullException(nameof(jsRuntime));
            }

            jsRuntime.Invoke<object>(identifier, args);
        }
    }
}
