// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.SignalR.Client;
public static partial class HubConnectionExtensions
{
    private static IDisposable On<TResult>(this HubConnection hubConnection, string methodName, Type[] parameterTypes, Func<object?[], TResult> handler)
    {
        return hubConnection.On(methodName, parameterTypes, static (parameters, state) =>
        {
            var currentHandler = (Func<object?[], TResult>)state;
            return Task.FromResult<object?>(currentHandler(parameters));
        }, handler);
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// Returns value returned by handler to server if the server requests a result.
    /// </summary>
    /// <typeparam name="TResult">The return type the handler returns.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="parameterTypes">The parameters types expected by the hub method.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<TResult>(this HubConnection hubConnection, string methodName, Type[] parameterTypes, Func<object?[], Task<TResult>> handler)
    {
        return hubConnection.On(methodName, parameterTypes, static async (parameters, state) =>
        {
            var currentHandler = (Func<object?[], Task<TResult>>)state;
            return await currentHandler(parameters).ConfigureAwait(false);
        }, handler);
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// Returns value returned by handler to server if the server requests a result.
    /// </summary>
    /// <typeparam name="TResult">The return type the handler returns.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<TResult>(this HubConnection hubConnection, string methodName, Func<Task<TResult>> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName, Type.EmptyTypes, args => handler());
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// Returns value returned by handler to server if the server requests a result.
    /// </summary>
    /// <typeparam name="TResult">The return type the handler returns.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<TResult>(this HubConnection hubConnection, string methodName, Func<TResult> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName, Type.EmptyTypes, args => handler());
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// Returns value returned by handler to server if the server requests a result.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="TResult">The return type the handler returns.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, TResult>(this HubConnection hubConnection, string methodName, Func<T1, TResult> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1) },
            args => handler((T1)args[0]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// Returns value returned by handler to server if the server requests a result.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="T2">The second argument type.</typeparam>
    /// <typeparam name="TResult">The return type the handler returns.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, T2, TResult>(this HubConnection hubConnection, string methodName, Func<T1, T2, TResult> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1), typeof(T2) },
            args => handler((T1)args[0]!, (T2)args[1]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// Returns value returned by handler to server if the server requests a result.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="T2">The second argument type.</typeparam>
    /// <typeparam name="T3">The third argument type.</typeparam>
    /// <typeparam name="TResult">The return type the handler returns.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, T2, T3, TResult>(this HubConnection hubConnection, string methodName, Func<T1, T2, T3, TResult> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1), typeof(T2), typeof(T3) },
            args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// Returns value returned by handler to server if the server requests a result.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="T2">The second argument type.</typeparam>
    /// <typeparam name="T3">The third argument type.</typeparam>
    /// <typeparam name="T4">The fourth argument type.</typeparam>
    /// <typeparam name="TResult">The return type the handler returns.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, T2, T3, T4, TResult>(this HubConnection hubConnection, string methodName, Func<T1, T2, T3, T4, TResult> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) },
            args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// Returns value returned by handler to server if the server requests a result.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="T2">The second argument type.</typeparam>
    /// <typeparam name="T3">The third argument type.</typeparam>
    /// <typeparam name="T4">The fourth argument type.</typeparam>
    /// <typeparam name="T5">The fifth argument type.</typeparam>
    /// <typeparam name="TResult">The return type the handler returns.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, T2, T3, T4, T5, TResult>(this HubConnection hubConnection, string methodName, Func<T1, T2, T3, T4, T5, TResult> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) },
            args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!, (T5)args[4]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// Returns value returned by handler to server if the server requests a result.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="T2">The second argument type.</typeparam>
    /// <typeparam name="T3">The third argument type.</typeparam>
    /// <typeparam name="T4">The fourth argument type.</typeparam>
    /// <typeparam name="T5">The fifth argument type.</typeparam>
    /// <typeparam name="T6">The sixth argument type.</typeparam>
    /// <typeparam name="TResult">The return type the handler returns.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, T2, T3, T4, T5, T6, TResult>(this HubConnection hubConnection, string methodName, Func<T1, T2, T3, T4, T5, T6, TResult> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6) },
            args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!, (T5)args[4]!, (T6)args[5]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// Returns value returned by handler to server if the server requests a result.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="T2">The second argument type.</typeparam>
    /// <typeparam name="T3">The third argument type.</typeparam>
    /// <typeparam name="T4">The fourth argument type.</typeparam>
    /// <typeparam name="T5">The fifth argument type.</typeparam>
    /// <typeparam name="T6">The sixth argument type.</typeparam>
    /// <typeparam name="T7">The seventh argument type.</typeparam>
    /// <typeparam name="TResult">The return type the handler returns.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, T2, T3, T4, T5, T6, T7, TResult>(this HubConnection hubConnection, string methodName, Func<T1, T2, T3, T4, T5, T6, T7, TResult> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7) },
            args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!, (T5)args[4]!, (T6)args[5]!, (T7)args[6]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// Returns value returned by handler to server if the server requests a result.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="T2">The second argument type.</typeparam>
    /// <typeparam name="T3">The third argument type.</typeparam>
    /// <typeparam name="T4">The fourth argument type.</typeparam>
    /// <typeparam name="T5">The fifth argument type.</typeparam>
    /// <typeparam name="T6">The sixth argument type.</typeparam>
    /// <typeparam name="T7">The seventh argument type.</typeparam>
    /// <typeparam name="T8">The eighth argument type.</typeparam>
    /// <typeparam name="TResult">The return type the handler returns.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(this HubConnection hubConnection, string methodName, Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8) },
            args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!, (T5)args[4]!, (T6)args[5]!, (T7)args[6]!, (T8)args[7]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// Returns value returned by handler to server if the server requests a result.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="TResult">The return type the handler returns.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, TResult>(this HubConnection hubConnection, string methodName, Func<T1, Task<TResult>> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1) },
            args => handler((T1)args[0]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// Returns value returned by handler to server if the server requests a result.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="T2">The second argument type.</typeparam>
    /// <typeparam name="TResult">The return type the handler returns.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, T2, TResult>(this HubConnection hubConnection, string methodName, Func<T1, T2, Task<TResult>> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1), typeof(T2) },
            args => handler((T1)args[0]!, (T2)args[1]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// Returns value returned by handler to server if the server requests a result.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="T2">The second argument type.</typeparam>
    /// <typeparam name="T3">The third argument type.</typeparam>
    /// <typeparam name="TResult">The return type the handler returns.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, T2, T3, TResult>(this HubConnection hubConnection, string methodName, Func<T1, T2, T3, Task<TResult>> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1), typeof(T2), typeof(T3) },
            args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// Returns value returned by handler to server if the server requests a result.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="T2">The second argument type.</typeparam>
    /// <typeparam name="T3">The third argument type.</typeparam>
    /// <typeparam name="T4">The fourth argument type.</typeparam>
    /// <typeparam name="TResult">The return type the handler returns.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, T2, T3, T4, TResult>(this HubConnection hubConnection, string methodName, Func<T1, T2, T3, T4, Task<TResult>> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) },
            args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// Returns value returned by handler to server if the server requests a result.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="T2">The second argument type.</typeparam>
    /// <typeparam name="T3">The third argument type.</typeparam>
    /// <typeparam name="T4">The fourth argument type.</typeparam>
    /// <typeparam name="T5">The fifth argument type.</typeparam>
    /// <typeparam name="TResult">The return type the handler returns.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, T2, T3, T4, T5, TResult>(this HubConnection hubConnection, string methodName, Func<T1, T2, T3, T4, T5, Task<TResult>> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) },
            args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!, (T5)args[4]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// Returns value returned by handler to server if the server requests a result.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="T2">The second argument type.</typeparam>
    /// <typeparam name="T3">The third argument type.</typeparam>
    /// <typeparam name="T4">The fourth argument type.</typeparam>
    /// <typeparam name="T5">The fifth argument type.</typeparam>
    /// <typeparam name="T6">The sixth argument type.</typeparam>
    /// <typeparam name="TResult">The return type the handler returns.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, T2, T3, T4, T5, T6, TResult>(this HubConnection hubConnection, string methodName, Func<T1, T2, T3, T4, T5, T6, Task<TResult>> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6) },
            args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!, (T5)args[4]!, (T6)args[5]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// Returns value returned by handler to server if the server requests a result.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="T2">The second argument type.</typeparam>
    /// <typeparam name="T3">The third argument type.</typeparam>
    /// <typeparam name="T4">The fourth argument type.</typeparam>
    /// <typeparam name="T5">The fifth argument type.</typeparam>
    /// <typeparam name="T6">The sixth argument type.</typeparam>
    /// <typeparam name="T7">The seventh argument type.</typeparam>
    /// <typeparam name="TResult">The return type the handler returns.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, T2, T3, T4, T5, T6, T7, TResult>(this HubConnection hubConnection, string methodName, Func<T1, T2, T3, T4, T5, T6, T7, Task<TResult>> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7) },
            args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!, (T5)args[4]!, (T6)args[5]!, (T7)args[6]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// Returns value returned by handler to server if the server requests a result.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="T2">The second argument type.</typeparam>
    /// <typeparam name="T3">The third argument type.</typeparam>
    /// <typeparam name="T4">The fourth argument type.</typeparam>
    /// <typeparam name="T5">The fifth argument type.</typeparam>
    /// <typeparam name="T6">The sixth argument type.</typeparam>
    /// <typeparam name="T7">The seventh argument type.</typeparam>
    /// <typeparam name="T8">The eighth argument type.</typeparam>
    /// <typeparam name="TResult">The return type the handler returns.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(this HubConnection hubConnection, string methodName, Func<T1, T2, T3, T4, T5, T6, T7, T8, Task<TResult>> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8) },
            args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!, (T5)args[4]!, (T6)args[5]!, (T7)args[6]!, (T8)args[7]!));
    }
}
