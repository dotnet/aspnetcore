// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
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
        /// <typeparam name="TValue">The JSON-serializable return type.</typeparam>
        /// <param name="identifier">An identifier for the function to invoke. For example, the value <code>"someScope.someFunction"</code> will invoke the function <code>window.someScope.someFunction</code>.</param>
        /// <param name="args">JSON-serializable arguments.</param>
        /// <returns>An instance of <typeparamref name="TValue"/> obtained by JSON-deserializing the return value.</returns>
        Task<TValue> InvokeAsync<TValue>(string identifier, params object[] args);

        /// <summary>
        /// Invokes the specified JavaScript function asynchronously.
        /// </summary>
        /// <typeparam name="TValue">The JSON-serializable return type.</typeparam>
        /// <param name="identifier">An identifier for the function to invoke. For example, the value <code>"someScope.someFunction"</code> will invoke the function <code>window.someScope.someFunction</code>.</param>
        /// <param name="args">JSON-serializable arguments.</param>
        /// <param name="cancellationToken">A cancellation token to signal the cancellation of the operation.</param>
        /// <returns>An instance of <typeparamref name="TValue"/> obtained by JSON-deserializing the return value.</returns>
        Task<TValue> InvokeAsync<TValue>(string identifier, IEnumerable<object> args, CancellationToken cancellationToken = default);
    }
}
