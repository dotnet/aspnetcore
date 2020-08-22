// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json;
using Microsoft.JSInterop.Infrastructure;
using WebAssembly.JSInterop;

namespace Microsoft.JSInterop.WebAssembly
{
    /// <summary>
    /// Provides methods for invoking JavaScript functions for applications running
    /// on the Mono WebAssembly runtime.
    /// </summary>
    public abstract class WebAssemblyJSRuntime : JSInProcessRuntime, IJSUnmarshalledRuntime
    {
        /// <inheritdoc />
        protected override string InvokeJS(string identifier, string argsJson, JSCallResultType resultType, long targetInstanceId)
        {
            var callInfo = new JSCallInfo
            {
                FunctionIdentifier = identifier,
                TargetInstanceId = targetInstanceId,
                ResultType = resultType,
                MarshalledCallArgsJson = argsJson ?? "[]",
                MarshalledCallAsyncHandle = default
            };

            var result = InternalCalls.InvokeJS<object, object, object, string>(out var exception, ref callInfo, null, null, null);

            return exception != null
                ? throw new JSException(exception)
                : result;
        }

        /// <inheritdoc />
        protected override void BeginInvokeJS(long asyncHandle, string identifier, string argsJson, JSCallResultType resultType, long targetInstanceId)
        {
            var callInfo = new JSCallInfo
            {
                FunctionIdentifier = identifier,
                TargetInstanceId = targetInstanceId,
                ResultType = resultType,
                MarshalledCallArgsJson = argsJson ?? "[]",
                MarshalledCallAsyncHandle = asyncHandle
            };

            InternalCalls.InvokeJS<object, object, object, string>(out _, ref callInfo, null, null, null);
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
            BeginInvokeJS(0, "DotNet.jsCallDispatcher.endInvokeDotNetFromJS", args, JSCallResultType.Default, 0);
        }

        /// <inheritdoc />
        TResult IJSUnmarshalledRuntime.InvokeUnmarshalled<TResult>(string identifier)
            => ((IJSUnmarshalledRuntime)this).InvokeUnmarshalled<object, object, object, TResult>(identifier, null, null, null);

        /// <inheritdoc />
        TResult IJSUnmarshalledRuntime.InvokeUnmarshalled<T0, TResult>(string identifier, T0 arg0)
            => ((IJSUnmarshalledRuntime)this).InvokeUnmarshalled<T0, object, object, TResult>(identifier, arg0, null, null);

        /// <inheritdoc />
        TResult IJSUnmarshalledRuntime.InvokeUnmarshalled<T0, T1, TResult>(string identifier, T0 arg0, T1 arg1)
            => ((IJSUnmarshalledRuntime)this).InvokeUnmarshalled<T0, T1, object, TResult>(identifier, arg0, arg1, null);

        /// <inheritdoc />
        TResult IJSUnmarshalledRuntime.InvokeUnmarshalled<T0, T1, T2, TResult>(string identifier, T0 arg0, T1 arg1, T2 arg2)
        {
            var callInfo = new JSCallInfo
            {
                FunctionIdentifier = identifier,
                ResultType = ResultTypeFromGeneric<TResult>()
            };

            var result = InternalCalls.InvokeJS<T0, T1, T2, TResult>(out var exception, ref callInfo, arg0, arg1, arg2);

            return exception != null
                ? throw new JSException(exception)
                : result;
        }
    }
}
