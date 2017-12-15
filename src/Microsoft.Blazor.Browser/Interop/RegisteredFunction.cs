// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using WebAssembly;

namespace Microsoft.Blazor.Browser.Interop
{
    /// <summary>
    /// Provides methods for invoking preregistered JavaScript functions from .NET code.
    /// </summary>
    public static class RegisteredFunction
    {
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
        public static TRes Invoke<TRes>(string identifier, params object[] args)
        {
            var result = Runtime.InvokeJSArray<TRes>(out var exception, identifier, args);
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
        public static TRes Invoke<T0, TRes>(string identifier)
        {
            var result = Runtime.InvokeJS<object, object, object, TRes>(out var exception, identifier, null, null, null);
            return exception != null
                ? throw new JavaScriptException(exception)
                : result;
        }

        /// <summary>
        /// Invokes the JavaScript function registered with the specified identifier.
        /// </summary>
        /// <typeparam name="T0">The type of the first argument.</typeparam>
        /// <typeparam name="TRes">The .NET type corresponding to the function's return value type.</typeparam>
        /// <param name="identifier">The identifier used when registering the target function.</param>
        /// <param name="arg0">The first argument.</param>
        /// <returns>The result of the function invocation.</returns>
        public static TRes Invoke<T0, TRes>(string identifier, T0 arg0)
        {
            var result = Runtime.InvokeJS<T0, object, object, TRes>(out var exception, identifier, arg0, null, null);
            return exception != null
                ? throw new JavaScriptException(exception)
                : result;
        }

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
        public static TRes Invoke<T0, T1, TRes>(string identifier, T0 arg0, T1 arg1)
        {
            var result = Runtime.InvokeJS<T0, T1, object, TRes>(out var exception, identifier, arg0, arg1, null);
            return exception != null
                ? throw new JavaScriptException(exception)
                : result;
        }

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
        public static TRes Invoke<T0, T1, T2, TRes>(string identifier, T0 arg0, T1 arg1, T2 arg2)
        {
            var result = Runtime.InvokeJS<T0, T1, T2, TRes>(out var exception, identifier, arg0, arg1, arg2);
            return exception != null
                ? throw new JavaScriptException(exception)
                : result;
        }
    }
}
