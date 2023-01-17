// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.JSInterop;

/// <summary>
/// Represents a reference to a JavaScript object whose functions can be invoked synchronously without JSON marshalling.
/// </summary>
public interface IJSUnmarshalledObjectReference : IJSInProcessObjectReference
{
    /// <summary>
    /// Invokes the JavaScript function registered with the specified identifier.
    /// </summary>
    /// <typeparam name="TResult">The .NET type corresponding to the function's return value type.</typeparam>
    /// <param name="identifier">The identifier used when registering the target function.</param>
    /// <returns>The result of the function invocation.</returns>
    [Obsolete("This method is obsolete. Use JSImportAttribute instead.")]
    TResult InvokeUnmarshalled<TResult>(string identifier);

    /// <summary>
    /// Invokes the JavaScript function registered with the specified identifier.
    /// </summary>
    /// <typeparam name="T0">The type of the first argument.</typeparam>
    /// <typeparam name="TResult">The .NET type corresponding to the function's return value type.</typeparam>
    /// <param name="identifier">The identifier used when registering the target function.</param>
    /// <param name="arg0">The first argument.</param>
    /// <returns>The result of the function invocation.</returns>
    [Obsolete("This method is obsolete. Use JSImportAttribute instead.")]
    TResult InvokeUnmarshalled<T0, TResult>(string identifier, T0 arg0);

    /// <summary>
    /// Invokes the JavaScript function registered with the specified identifier.
    /// </summary>
    /// <typeparam name="T0">The type of the first argument.</typeparam>
    /// <typeparam name="T1">The type of the second argument.</typeparam>
    /// <typeparam name="TResult">The .NET type corresponding to the function's return value type.</typeparam>
    /// <param name="identifier">The identifier used when registering the target function.</param>
    /// <param name="arg0">The first argument.</param>
    /// <param name="arg1">The second argument.</param>
    /// <returns>The result of the function invocation.</returns>
    [Obsolete("This method is obsolete. Use JSImportAttribute instead.")]
    TResult InvokeUnmarshalled<T0, T1, TResult>(string identifier, T0 arg0, T1 arg1);

    /// <summary>
    /// Invokes the JavaScript function registered with the specified identifier.
    /// </summary>
    /// <typeparam name="T0">The type of the first argument.</typeparam>
    /// <typeparam name="T1">The type of the second argument.</typeparam>
    /// <typeparam name="T2">The type of the third argument.</typeparam>
    /// <typeparam name="TResult">The .NET type corresponding to the function's return value type.</typeparam>
    /// <param name="identifier">The identifier used when registering the target function.</param>
    /// <param name="arg0">The first argument.</param>
    /// <param name="arg1">The second argument.</param>
    /// <param name="arg2">The third argument.</param>
    /// <returns>The result of the function invocation.</returns>
    [Obsolete("This method is obsolete. Use JSImportAttribute instead.")]
    TResult InvokeUnmarshalled<T0, T1, T2, TResult>(string identifier, T0 arg0, T1 arg1, T2 arg2);
}
