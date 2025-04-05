// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.JSInterop;

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
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    TValue Invoke<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string identifier, params object?[]? args);

    /// <summary>
    /// Invokes the specified JavaScript constructor function synchronously. The function is invoked with the <c>new</c> operator.
    /// </summary>
    /// <param name="identifier">An identifier for the constructor function to invoke. For example, the value <c>"someScope.SomeClass"</c> will invoke the constructor <c>window.someScope.SomeClass</c>.</param>
    /// <param name="args">JSON-serializable arguments.</param>
    /// <returns>An <see cref="IJSInProcessObjectReference"/> instance that represents the created JS object.</returns>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    IJSInProcessObjectReference InvokeNew(string identifier, object?[]? args);

    /// <summary>
    /// Reads the value of the specified JavaScript property synchronously.
    /// </summary>
    /// <typeparam name="TValue">The JSON-serializable return type.</typeparam>
    /// <param name="identifier">An identifier for the property to read. For example, the value <c>"someScope.someProp"</c> will read the value of the property <c>window.someScope.someProp</c>.</param>
    /// <returns>An instance of <typeparamref name="TValue"/> obtained by JSON-deserializing the return value.</returns>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    TValue GetValue<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string identifier);

    /// <summary>
    /// Updates the value of the specified JavaScript property synchronously. If the property is not defined on the target object, it will be created.
    /// </summary>
    /// <typeparam name="TValue">JSON-serializable argument type.</typeparam>
    /// <param name="identifier">An identifier for the property to set. For example, the value <c>"someScope.someProp"</c> will update the property <c>window.someScope.someProp</c>.</param>
    /// <param name="value">JSON-serializable value.</param>
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed.")]
    void SetValue<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string identifier, TValue value);
}
