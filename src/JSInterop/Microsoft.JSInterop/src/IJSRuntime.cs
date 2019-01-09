// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Represents an instance of a JavaScript runtime to which calls may be dispatched.
    /// </summary>
    public interface IJSRuntime
    {
        /// <summary>
        /// Invokes the specified JavaScript function asynchronously.
        /// </summary>
        /// <typeparam name="T">The JSON-serializable return type.</typeparam>
        /// <param name="identifier">An identifier for the function to invoke. For example, the value <code>"someScope.someFunction"</code> will invoke the function <code>window.someScope.someFunction</code>.</param>
        /// <param name="args">JSON-serializable arguments.</param>
        /// <returns>An instance of <typeparamref name="T"/> obtained by JSON-deserializing the return value.</returns>
        Task<T> InvokeAsync<T>(string identifier, params object[] args);

        /// <summary>
        /// Stops tracking the .NET object represented by the <see cref="DotNetObjectRef"/>.
        /// This allows it to be garbage collected (if nothing else holds a reference to it)
        /// and means the JS-side code can no longer invoke methods on the instance or pass
        /// it as an argument to subsequent calls.
        /// </summary>
        /// <param name="dotNetObjectRef">The reference to stop tracking.</param>
        /// <remarks>This method is called automatically by <see cref="DotNetObjectRef.Dispose"/>.</remarks>
        void UntrackObjectRef(DotNetObjectRef dotNetObjectRef);
    }
}
