// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using WebAssembly.JSInterop;

namespace Mono.WebAssembly.Interop
{
    /// <summary>
    /// Provides methods for invoking JavaScript functions for applications running
    /// on the Mono WebAssembly runtime.
    /// </summary>
    public class MonoWebAssemblyJSRuntime : JSInProcessRuntime
    {
        /// <summary>
        /// Gets the <see cref="MonoWebAssemblyJSRuntime"/> used to perform operations using <see cref="DotNetDispatcher"/>.
        /// </summary>
        private static MonoWebAssemblyJSRuntime Instance { get; set; }

        /// <summary>
        /// Initializes the <see cref="MonoWebAssemblyJSRuntime"/> to be used to perform operations using <see cref="DotNetDispatcher"/>.
        /// </summary>
        /// <param name="jsRuntime">The <see cref="MonoWebAssemblyJSRuntime"/> instance.</param>
        protected static void Initialize(MonoWebAssemblyJSRuntime jsRuntime)
        {
            if (Instance != null)
            {
                throw new InvalidOperationException("MonoWebAssemblyJSRuntime has already been initialized.");
            }

            Instance = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        }

        /// <inheritdoc />
        protected override string InvokeJS(string identifier, string argsJson)
        {
            var noAsyncHandle = default(long);
            var result = InternalCalls.InvokeJSMarshalled(out var exception, ref noAsyncHandle, identifier, argsJson);
            return exception != null
                ? throw new JSException(exception)
                : result;
        }

        /// <inheritdoc />
        protected override void BeginInvokeJS(long asyncHandle, string identifier, string argsJson)
        {
            InternalCalls.InvokeJSMarshalled(out _, ref asyncHandle, identifier, argsJson);
        }

        // Invoked via Mono's JS interop mechanism (invoke_method)
        private static string InvokeDotNet(string assemblyName, string methodIdentifier, string dotNetObjectId, string argsJson)
        {
            var callInfo = new DotNetInvocationInfo(assemblyName, methodIdentifier, dotNetObjectId == null ? default : long.Parse(dotNetObjectId), callId: null);
            return DotNetDispatcher.Invoke(Instance, callInfo, argsJson);
        }

        // Invoked via Mono's JS interop mechanism (invoke_method)
        private static void EndInvokeJS(string argsJson)
            => DotNetDispatcher.EndInvokeJS(Instance, argsJson);

        // Invoked via Mono's JS interop mechanism (invoke_method)
        private static void BeginInvokeDotNet(string callId, string assemblyNameOrDotNetObjectId, string methodIdentifier, string argsJson)
        {
            // Figure out whether 'assemblyNameOrDotNetObjectId' is the assembly name or the instance ID
            // We only need one for any given call. This helps to work around the limitation that we can
            // only pass a maximum of 4 args in a call from JS to Mono WebAssembly.
            string assemblyName;
            long dotNetObjectId;
            if (char.IsDigit(assemblyNameOrDotNetObjectId[0]))
            {
                dotNetObjectId = long.Parse(assemblyNameOrDotNetObjectId);
                assemblyName = null;
            }
            else
            {
                dotNetObjectId = default;
                assemblyName = assemblyNameOrDotNetObjectId;
            }

            var callInfo = new DotNetInvocationInfo(assemblyName, methodIdentifier, dotNetObjectId, callId);
            DotNetDispatcher.BeginInvokeDotNet(Instance, callInfo, argsJson);
        }

        protected override void EndInvokeDotNet(DotNetInvocationInfo callInfo, in DotNetInvocationResult dispatchResult)
        {
            // For failures, the common case is to call EndInvokeDotNet with the Exception object.
            // For these we'll serialize as something that's useful to receive on the JS side.
            // If the value is not an Exception, we'll just rely on it being directly JSON-serializable.
            var resultOrError = dispatchResult.Success ? dispatchResult.Result : dispatchResult.Exception.ToString();

            // We pass 0 as the async handle because we don't want the JS-side code to
            // send back any notification (we're just providing a result for an existing async call)
            var args = JsonSerializer.Serialize(new[] { callInfo.CallId, dispatchResult.Success, resultOrError }, JsonSerializerOptions);
            BeginInvokeJS(0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", args);
        }

        #region Custom MonoWebAssemblyJSRuntime methods

        /// <summary>
        /// Invokes the JavaScript function registered with the specified identifier.
        /// </summary>
        /// <typeparam name="TRes">The .NET type corresponding to the function's return value type.</typeparam>
        /// <param name="identifier">The identifier used when registering the target function.</param>
        /// <returns>The result of the function invocation.</returns>
        public TRes InvokeUnmarshalled<TRes>(string identifier)
            => InvokeUnmarshalled<object, object, object, TRes>(identifier, null, null, null);

        /// <summary>
        /// Invokes the JavaScript function registered with the specified identifier.
        /// </summary>
        /// <typeparam name="T0">The type of the first argument.</typeparam>
        /// <typeparam name="TRes">The .NET type corresponding to the function's return value type.</typeparam>
        /// <param name="identifier">The identifier used when registering the target function.</param>
        /// <param name="arg0">The first argument.</param>
        /// <returns>The result of the function invocation.</returns>
        public TRes InvokeUnmarshalled<T0, TRes>(string identifier, T0 arg0)
            => InvokeUnmarshalled<T0, object, object, TRes>(identifier, arg0, null, null);

        /// <summary>
        /// Invokes the JavaScript function registered with the specified identifier.
        /// </summary>
        /// <typeparam name="T0">The type of the first argument.</typeparam>
        /// <typeparam name="T1">The type of the second argument.</typeparam>
        /// <typeparam name="TRes">The .NET type corresponding to the function's return value type.</typeparam>
        /// <param name="identifier">The identifier used when registering the target function.</param>
        /// <param name="arg0">The first argument.</param>
        /// <param name="arg1">The second argument.</param>
        /// <returns>The result of the function invocation.</returns>
        public TRes InvokeUnmarshalled<T0, T1, TRes>(string identifier, T0 arg0, T1 arg1)
            => InvokeUnmarshalled<T0, T1, object, TRes>(identifier, arg0, arg1, null);

        /// <summary>
        /// Invokes the JavaScript function registered with the specified identifier.
        /// </summary>
        /// <typeparam name="T0">The type of the first argument.</typeparam>
        /// <typeparam name="T1">The type of the second argument.</typeparam>
        /// <typeparam name="T2">The type of the third argument.</typeparam>
        /// <typeparam name="TRes">The .NET type corresponding to the function's return value type.</typeparam>
        /// <param name="identifier">The identifier used when registering the target function.</param>
        /// <param name="arg0">The first argument.</param>
        /// <param name="arg1">The second argument.</param>
        /// <param name="arg2">The third argument.</param>
        /// <returns>The result of the function invocation.</returns>
        public TRes InvokeUnmarshalled<T0, T1, T2, TRes>(string identifier, T0 arg0, T1 arg1, T2 arg2)
        {
            var result = InternalCalls.InvokeJSUnmarshalled<T0, T1, T2, TRes>(out var exception, identifier, arg0, arg1, arg2);
            return exception != null
                ? throw new JSException(exception)
                : result;
        }

        #endregion
    }
}
