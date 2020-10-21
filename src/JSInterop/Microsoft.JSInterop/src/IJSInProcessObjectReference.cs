// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Represents a reference to a JavaScript object whose functions can be invoked synchronously.
    /// </summary>
    public interface IJSInProcessObjectReference : IJSObjectReference, IDisposable
    {
        /// <summary>
        /// Invokes the specified JavaScript function synchronously.
        /// </summary>
        /// <typeparam name="TValue">The JSON-serializable return type.</typeparam>
        /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>someScope.someFunction</c> on the target instance.</param>
        /// <param name="args">JSON-serializable arguments.</param>
        /// <returns>An instance of <typeparamref name="TValue"/> obtained by JSON-deserializing the return value.</returns>
        TValue Invoke<TValue>(string identifier, params object?[]? args);

        /// <summary>
        /// Invokes the specified JavaScript function synchronously.
        /// </summary>
        /// <typeparam name="TValue">The JSON-serializable return type.</typeparam>
        /// <param name="identifier">An identifier for the function to invoke. For example, the value <c>"someScope.someFunction"</c> will invoke the function <c>someScope.someFunction</c> on the target instance.</param>
        /// <param name="jsonSerializerOptions">JSON serialization options to use during serialization/deserialization of the args and return value.</param>
        /// <param name="args">JSON-serializable arguments.</param>
        /// <returns>An instance of <typeparamref name="TValue"/> obtained by JSON-deserializing the return value.</returns>
        TValue Invoke<TValue>(string identifier, JsonSerializerOptions jsonSerializerOptions, params object?[]? args);
    }
}
