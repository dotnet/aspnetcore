// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.JSInterop.WebAssembly
{
    /// <summary>
    /// Extension methods for <see cref="WebAssemblyJSRuntime"/>.
    /// </summary>
    public static class WebAssemblyJSRuntimeExtensions
    {
        /// <summary>
        /// Invokes the JavaScript function registered with the specified identifier.
        /// </summary>
        /// <typeparam name="TResult">The .NET type corresponding to the function's return value type.</typeparam>
        /// <param name="jsRuntime">The <see cref="WebAssemblyJSRuntime"/>.</param>
        /// <param name="identifier">The identifier used when registering the target function.</param>
        /// <returns>The result of the function invocation.</returns>
        public static TResult InvokeUnmarshalled<TResult>(this WebAssemblyJSRuntime jsRuntime, string identifier)
        {
            if (jsRuntime is null)
            {
                throw new ArgumentNullException(nameof(jsRuntime));
            }

            return jsRuntime.InvokeUnmarshalled<object, object, object, TResult>(identifier, null, null, null);
        }

        /// <summary>
        /// Invokes the JavaScript function registered with the specified identifier.
        /// </summary>
        /// <typeparam name="T0">The type of the first argument.</typeparam>
        /// <typeparam name="TResult">The .NET type corresponding to the function's return value type.</typeparam>
        /// <param name="jsRuntime">The <see cref="WebAssemblyJSRuntime"/>.</param>
        /// <param name="identifier">The identifier used when registering the target function.</param>
        /// <param name="arg0">The first argument.</param>
        /// <returns>The result of the function invocation.</returns>
        public static TResult InvokeUnmarshalled<T0, TResult>(this WebAssemblyJSRuntime jsRuntime, string identifier, T0 arg0)
        {
            if (jsRuntime is null)
            {
                throw new ArgumentNullException(nameof(jsRuntime));
            }

            return jsRuntime.InvokeUnmarshalled<T0, object, object, TResult>(identifier, arg0, null, null);
        }

        /// <summary>
        /// Invokes the JavaScript function registered with the specified identifier.
        /// </summary>
        /// <typeparam name="T0">The type of the first argument.</typeparam>
        /// <typeparam name="T1">The type of the second argument.</typeparam>
        /// <typeparam name="TResult">The .NET type corresponding to the function's return value type.</typeparam>
        /// <param name="jsRuntime">The <see cref="WebAssemblyJSRuntime"/>.</param>
        /// <param name="identifier">The identifier used when registering the target function.</param>
        /// <param name="arg0">The first argument.</param>
        /// <param name="arg1">The second argument.</param>
        /// <returns>The result of the function invocation.</returns>
        public static TResult InvokeUnmarshalled<T0, T1, TResult>(this WebAssemblyJSRuntime jsRuntime, string identifier, T0 arg0, T1 arg1)
        {
            if (jsRuntime is null)
            {
                throw new ArgumentNullException(nameof(jsRuntime));
            }

            return jsRuntime.InvokeUnmarshalled<T0, T1, object, TResult>(identifier, arg0, arg1, null);
        }
    }
}
