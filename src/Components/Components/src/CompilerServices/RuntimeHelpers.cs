// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Components.CompilerServices;

/// <summary>
/// Used by generated code produced by the Components code generator. Not intended or supported
/// for use in application code.
/// </summary>
public static class RuntimeHelpers
{
    /// <summary>
    /// Not intended for use by application code.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    public static T TypeCheck<T>(T value) => value;

    /// <summary>
    /// Not intended for use by application code.
    /// </summary>
    /// <param name="receiver"></param>
    /// <param name="callback"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    //
    // This method is used with `@bind-Value` for components. When a component has a generic type, it's
    // really messy to write to try and write the parameter type for ValueChanged - because it can contain generic
    // type parameters. We're using a trick of type inference to generate the proper typing for the delegate
    // so that method-group-to-delegate conversion works.
    public static EventCallback<T> CreateInferredEventCallback<T>(object receiver, Action<T> callback, T value)
    {
        return EventCallback.Factory.Create<T>(receiver, callback);
    }

    /// <summary>
    /// Not intended for use by application code.
    /// </summary>
    /// <param name="receiver"></param>
    /// <param name="callback"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    //
    // This method is used with `@bind-Value` for components. When a component has a generic type, it's
    // really messy to write to try and write the parameter type for ValueChanged - because it can contain generic
    // type parameters. We're using a trick of type inference to generate the proper typing for the delegate
    // so that method-group-to-delegate conversion works.
    public static EventCallback<T> CreateInferredEventCallback<T>(object receiver, Func<T, Task> callback, T value)
    {
        return EventCallback.Factory.Create<T>(receiver, callback);
    }

    /// <summary>
    /// Not intended for use by application code.
    /// </summary>
    /// <param name="receiver"></param>
    /// <param name="callback"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    //
    // This method is used with `@bind-Value` for components. When a component has a generic type, it's
    // really messy to write the parameter type for ValueChanged - because it can contain generic
    // type parameters. We're using a trick of type inference to generate the proper typing for the delegate
    // so that method-group-to-delegate conversion works.
    public static EventCallback<T> CreateInferredEventCallback<T>(object receiver, EventCallback<T> callback, T value)
    {
        return EventCallback.Factory.Create<T>(receiver, callback);
    }

    /// <summary>
    /// Not intended for use by application code.
    /// </summary>
    /// <param name="callback"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    //
    // This method is used with `@bind-Value:set` to coerce the user provided expression to a Func<T, Task>.
    public static Func<T, Task> CreateInferredBindSetter<T>(Func<T, Task> callback, T value)
    {
        return callback;
    }

    /// <summary>
    /// Not intended for use by application code.
    /// </summary>
    /// <param name="callback"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    //
    // This method is used with `@bind-Value:set` to coerce the user provided expression to a Func<T, Task>.
    public static Func<T, Task> CreateInferredBindSetter<T>(Action<T?> callback, T value)
    {
        return (value) =>
        {
            callback(value);
            return Task.CompletedTask;
        };
    }

    /// <summary>
    /// Not intended for use by application code.
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    //
    // This method is used with `@bind-Value:after` for components. When :after is provided we don't know the
    // type of the expression provided by the developer or if we can invoke it directly, as it can be a lambda
    // and unlike in JavaScript, C# doesn't support Immediately Invoked Function Expressions so we need to pass
    // the expression to this helper method and invoke it inside.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvokeSynchronousDelegate(Action callback)
    {
        callback();
    }

    /// <summary>
    /// Not intended for use by application code.
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    //
    // This method is used with `@bind-Value:after` for components. When :after is provided we don't know the
    // type of the expression provided by the developer or if we can invoke it directly, as it can be a lambda
    // and unlike in JavaScript, C# doesn't support Immediately Invoked Function Expressions so we need to pass
    // the expression to this helper method and invoke it inside.
    // In addition to that, when the receiving target delegate property result is awaitable, we can receive either
    // an Action or a Func<Task> and we don't have that information at compile time, so we use this helper to
    // normalize both operations into a Task in the same way we do for EventCallback
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task InvokeAsynchronousDelegate(Action callback)
    {
        callback();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Not intended for use by application code.
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    //
    // This method is used with `@bind-Value:after` for components. When :after is provided we don't know the
    // type of the expression provided by the developer or if we can invoke it directly, as it can be a lambda
    // and unlike in JavaScript, C# doesn't support Immediately Invoked Function Expressions so we need to pass
    // the expression to this helper method and invoke it inside.
    // In addition to that, when the receiving target delegate property result is awaitable, we can receive either
    // an Action or a Func<Task> and we don't have that information at compile time, so we use this helper to
    // normalize both operations into a Task in the same way we do for EventCallback
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task InvokeAsynchronousDelegate(Func<Task> callback)
    {
        return callback();
    }
}
