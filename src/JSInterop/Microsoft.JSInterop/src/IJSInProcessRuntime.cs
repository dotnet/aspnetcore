// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json;

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Represents an instance of a JavaScript runtime to which calls may be dispatched.
    /// </summary>
    public interface IJSInProcessRuntime : IJSRuntime
    {
        /// <summary>
        /// Invokes the specified JavaScript function synchronously.
        /// </summary>
        /// <typeparam name="TResult">The JSON-serializable return type.</typeparam>
        /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
        /// <param name="args">JSON-serializable arguments.</param>
        /// <returns>An instance of <typeparamref name="TResult"/> obtained by JSON-deserializing the return value.</returns>
        TResult Invoke<TResult>(string identifier, params object?[]? args);

        /// <summary>
        /// Invokes the specified JavaScript function synchronously.
        /// </summary>
        /// <typeparam name="TResult">The JSON-serializable return type.</typeparam>
        /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>window.someScope.someFunction</c>.</param>
        /// <param name="jsonSerializerOptions">JSON serialization options to use during serialization/deserialization of the args and return value.</param>
        /// <param name="args">JSON-serializable arguments.</param>
        /// <returns>An instance of <typeparamref name="TResult"/> obtained by JSON-deserializing the return value.</returns>
        TResult Invoke<TResult>(string identifier, JsonSerializerOptions jsonSerializerOptions, params object?[]? args);
    }
}
