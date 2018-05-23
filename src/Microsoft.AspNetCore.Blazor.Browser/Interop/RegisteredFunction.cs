// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using WebAssembly;

namespace Microsoft.AspNetCore.Blazor.Browser.Interop
{
    /// <summary>
    /// Provides methods for invoking preregistered JavaScript functions from .NET code.
    /// </summary>
    public static class RegisteredFunction
    {
        /// <summary>
        /// Invokes the JavaScript function registered with the specified identifier.
        /// Arguments and return values are marshalled via JSON serialization.
        /// </summary>
        /// <typeparam name="TRes">The .NET type corresponding to the function's return value type. This type must be JSON deserializable.</typeparam>
        /// <param name="identifier">The identifier used when registering the target function.</param>
        /// <param name="args">The arguments to pass, each of which must be JSON serializable.</param>
        /// <returns>The result of the function invocation.</returns>
        public static TRes Invoke<TRes>(string identifier, params object[] args)
        {
            // This is a low-perf convenience method that bypasses the need to deal with
            // .NET memory and data structures on the JS side
            var argsJson = new string[args.Length + 1];

            argsJson[0] = identifier;
            for (int i = 0; i < args.Length; i++)
            {
                argsJson[i + 1] = JsonUtil.Serialize(args[i]);
            }

            var resultJson = InvokeUnmarshalled<string>("invokeWithJsonMarshalling", argsJson);

            var result = JsonUtil.Deserialize<InvocationResult<TRes>>(resultJson);
            if (result.Succeeded)
            {
                return result.Result;
            }
            else
            {
                throw new JavaScriptException(result.Message);
            }
        }

        /// <summary>
        /// Invokes the JavaScript function registered with the specified identifier.
        /// Arguments and return values are marshalled via JSON serialization.
        /// </summary>
        /// <typeparam name="TRes">The .NET type corresponding to the function's return value type. This type must be JSON deserializable.</typeparam>
        /// <param name="identifier">The identifier used when registering the target function.</param>
        /// <param name="args">The arguments to pass, each of which must be JSON serializable.</param>
        /// <returns>The result of the function invocation.</returns>
        public static Task<TRes> InvokeAsync<TRes>(string identifier, params object[] args)
        {
            var tcs = new TaskCompletionSource<TRes>();
            var callbackId = Guid.NewGuid().ToString();
            var argsJson = new string[args.Length + 2];

            argsJson[0] = identifier;
            argsJson[1] = callbackId;
            for (int i = 0; i < args.Length; i++)
            {
                argsJson[i + 2] = JsonUtil.Serialize(args[i]);
            }

            TaskCallbacks.Track(callbackId, new Action<string>(r =>
            {
                var res = JsonUtil.Deserialize<InvocationResult<TRes>>(r);
                TaskCallbacks.Untrack(callbackId);
                if (res.Succeeded)
                {
                    tcs.SetResult(res.Result);
                }
                else
                {
                    tcs.SetException(new JavaScriptException(res.Message));
                }
            }));

            try
            {
                var result = Invoke<object>("invokeWithJsonMarshallingAsync", argsJson);

            }
            catch
            {
                TaskCallbacks.Untrack(callbackId);
                throw;
            }

            return tcs.Task;
        }

        /// <summary>
        /// Invokes the JavaScript function registered with the specified identifier.
        /// 
        /// When using this overload, all arguments will be supplied as <see cref="System.Object" />
        /// references, meaning that any reference types will be boxed. If you are passing
        /// 3 or fewer arguments, it is preferable to instead call the overload that
        /// specifies generic type arguments for each argument.
        /// </summary>
        /// <typeparam name="TRes">The .NET type corresponding to the function's return value type.</typeparam>
        /// <param name="identifier">The identifier used when registering the target function.</param>
        /// <param name="args">The arguments to pass, each of which will be supplied as a <see cref="System.Object" /> instance.</param>
        /// <returns>The result of the function invocation.</returns>
        public static TRes InvokeUnmarshalled<TRes>(string identifier, params object[] args)
        {
            var result = Runtime.BlazorInvokeJSArray<TRes>(out var exception, identifier, args);
            return exception != null
                ? throw new JavaScriptException(exception)
                : result;
        }

        /// <summary>
        /// Invokes the JavaScript function registered with the specified identifier.
        /// </summary>
        /// <typeparam name="TRes">The .NET type corresponding to the function's return value type.</typeparam>
        /// <param name="identifier">The identifier used when registering the target function.</param>
        /// <returns>The result of the function invocation.</returns>
        public static TRes InvokeUnmarshalled<TRes>(string identifier)
            => InvokeUnmarshalled<object, object, object, TRes>(identifier, null, null, null);

        /// <summary>
        /// Invokes the JavaScript function registered with the specified identifier.
        /// </summary>
        /// <typeparam name="T0">The type of the first argument.</typeparam>
        /// <typeparam name="TRes">The .NET type corresponding to the function's return value type.</typeparam>
        /// <param name="identifier">The identifier used when registering the target function.</param>
        /// <param name="arg0">The first argument.</param>
        /// <returns>The result of the function invocation.</returns>
        public static TRes InvokeUnmarshalled<T0, TRes>(string identifier, T0 arg0)
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
        public static TRes InvokeUnmarshalled<T0, T1, TRes>(string identifier, T0 arg0, T1 arg1)
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
        public static TRes InvokeUnmarshalled<T0, T1, T2, TRes>(string identifier, T0 arg0, T1 arg1, T2 arg2)
        {
            var result = Runtime.BlazorInvokeJS<T0, T1, T2, TRes>(out var exception, identifier, arg0, arg1, arg2);
            return exception != null
                ? throw new JavaScriptException(exception)
                : result;
        }
    }

    internal class TaskCallback
    {
        public static void InvokeTaskCallback(string id, string result)
        {
            var callback = TaskCallbacks.Get(id);
            callback(result);
        }
    }
}
