// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.SignalR.Client;

/// <summary>
/// Extension methods for <see cref="HubConnection"/>.
/// </summary>
public static partial class HubConnectionExtensions
{
    private static IDisposable On(this HubConnection hubConnection, string methodName, Type[] parameterTypes, Action<object?[]> handler)
    {
        return hubConnection.On(methodName, parameterTypes, static (parameters, state) =>
        {
            var currentHandler = (Action<object?[]>)state;
            currentHandler(parameters);
            return Task.CompletedTask;
        }, handler);
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// </summary>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On(this HubConnection hubConnection, string methodName, Action handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName, Type.EmptyTypes, args => handler());
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1>(this HubConnection hubConnection, string methodName, Action<T1> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1) },
            args => handler((T1)args[0]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="T2">The second argument type.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, T2>(this HubConnection hubConnection, string methodName, Action<T1, T2> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1), typeof(T2) },
            args => handler((T1)args[0]!, (T2)args[1]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="T2">The second argument type.</typeparam>
    /// <typeparam name="T3">The third argument type.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, T2, T3>(this HubConnection hubConnection, string methodName, Action<T1, T2, T3> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1), typeof(T2), typeof(T3) },
            args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="T2">The second argument type.</typeparam>
    /// <typeparam name="T3">The third argument type.</typeparam>
    /// <typeparam name="T4">The fourth argument type.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, T2, T3, T4>(this HubConnection hubConnection, string methodName, Action<T1, T2, T3, T4> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) },
            args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="T2">The second argument type.</typeparam>
    /// <typeparam name="T3">The third argument type.</typeparam>
    /// <typeparam name="T4">The fourth argument type.</typeparam>
    /// <typeparam name="T5">The fifth argument type.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, T2, T3, T4, T5>(this HubConnection hubConnection, string methodName, Action<T1, T2, T3, T4, T5> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) },
            args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!, (T5)args[4]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="T2">The second argument type.</typeparam>
    /// <typeparam name="T3">The third argument type.</typeparam>
    /// <typeparam name="T4">The fourth argument type.</typeparam>
    /// <typeparam name="T5">The fifth argument type.</typeparam>
    /// <typeparam name="T6">The sixth argument type.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, T2, T3, T4, T5, T6>(this HubConnection hubConnection, string methodName, Action<T1, T2, T3, T4, T5, T6> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6) },
            args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!, (T5)args[4]!, (T6)args[5]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="T2">The second argument type.</typeparam>
    /// <typeparam name="T3">The third argument type.</typeparam>
    /// <typeparam name="T4">The fourth argument type.</typeparam>
    /// <typeparam name="T5">The fifth argument type.</typeparam>
    /// <typeparam name="T6">The sixth argument type.</typeparam>
    /// <typeparam name="T7">The seventh argument type.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, T2, T3, T4, T5, T6, T7>(this HubConnection hubConnection, string methodName, Action<T1, T2, T3, T4, T5, T6, T7> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7) },
            args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!, (T5)args[4]!, (T6)args[5]!, (T7)args[6]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="T2">The second argument type.</typeparam>
    /// <typeparam name="T3">The third argument type.</typeparam>
    /// <typeparam name="T4">The fourth argument type.</typeparam>
    /// <typeparam name="T5">The fifth argument type.</typeparam>
    /// <typeparam name="T6">The sixth argument type.</typeparam>
    /// <typeparam name="T7">The seventh argument type.</typeparam>
    /// <typeparam name="T8">The eighth argument type.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, T2, T3, T4, T5, T6, T7, T8>(this HubConnection hubConnection, string methodName, Action<T1, T2, T3, T4, T5, T6, T7, T8> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8) },
            args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!, (T5)args[4]!, (T6)args[5]!, (T7)args[6]!, (T8)args[7]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// </summary>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="parameterTypes">The parameters types expected by the hub method.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On(this HubConnection hubConnection, string methodName, Type[] parameterTypes, Func<object?[], Task> handler)
    {
        return hubConnection.On(methodName, parameterTypes, (parameters, state) =>
        {
            var currentHandler = (Func<object?[], Task>)state;
            return currentHandler(parameters);
        }, handler);
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// </summary>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On(this HubConnection hubConnection, string methodName, Func<Task> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName, Type.EmptyTypes, args => handler());
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1>(this HubConnection hubConnection, string methodName, Func<T1, Task> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1) },
            args => handler((T1)args[0]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="T2">The second argument type.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, T2>(this HubConnection hubConnection, string methodName, Func<T1, T2, Task> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1), typeof(T2) },
            args => handler((T1)args[0]!, (T2)args[1]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="T2">The second argument type.</typeparam>
    /// <typeparam name="T3">The third argument type.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, T2, T3>(this HubConnection hubConnection, string methodName, Func<T1, T2, T3, Task> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1), typeof(T2), typeof(T3) },
            args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="T2">The second argument type.</typeparam>
    /// <typeparam name="T3">The third argument type.</typeparam>
    /// <typeparam name="T4">The fourth argument type.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, T2, T3, T4>(this HubConnection hubConnection, string methodName, Func<T1, T2, T3, T4, Task> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) },
            args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="T2">The second argument type.</typeparam>
    /// <typeparam name="T3">The third argument type.</typeparam>
    /// <typeparam name="T4">The fourth argument type.</typeparam>
    /// <typeparam name="T5">The fifth argument type.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, T2, T3, T4, T5>(this HubConnection hubConnection, string methodName, Func<T1, T2, T3, T4, T5, Task> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) },
            args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!, (T5)args[4]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="T2">The second argument type.</typeparam>
    /// <typeparam name="T3">The third argument type.</typeparam>
    /// <typeparam name="T4">The fourth argument type.</typeparam>
    /// <typeparam name="T5">The fifth argument type.</typeparam>
    /// <typeparam name="T6">The sixth argument type.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, T2, T3, T4, T5, T6>(this HubConnection hubConnection, string methodName, Func<T1, T2, T3, T4, T5, T6, Task> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6) },
            args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!, (T5)args[4]!, (T6)args[5]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="T2">The second argument type.</typeparam>
    /// <typeparam name="T3">The third argument type.</typeparam>
    /// <typeparam name="T4">The fourth argument type.</typeparam>
    /// <typeparam name="T5">The fifth argument type.</typeparam>
    /// <typeparam name="T6">The sixth argument type.</typeparam>
    /// <typeparam name="T7">The seventh argument type.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, T2, T3, T4, T5, T6, T7>(this HubConnection hubConnection, string methodName, Func<T1, T2, T3, T4, T5, T6, T7, Task> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7) },
            args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!, (T5)args[4]!, (T6)args[5]!, (T7)args[6]!));
    }

    /// <summary>
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    /// </summary>
    /// <typeparam name="T1">The first argument type.</typeparam>
    /// <typeparam name="T2">The second argument type.</typeparam>
    /// <typeparam name="T3">The third argument type.</typeparam>
    /// <typeparam name="T4">The fourth argument type.</typeparam>
    /// <typeparam name="T5">The fifth argument type.</typeparam>
    /// <typeparam name="T6">The sixth argument type.</typeparam>
    /// <typeparam name="T7">The seventh argument type.</typeparam>
    /// <typeparam name="T8">The eighth argument type.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the hub method to define.</param>
    /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
    /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
    public static IDisposable On<T1, T2, T3, T4, T5, T6, T7, T8>(this HubConnection hubConnection, string methodName, Func<T1, T2, T3, T4, T5, T6, T7, T8, Task> handler)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        return hubConnection.On(methodName,
            new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8) },
            args => handler((T1)args[0]!, (T2)args[1]!, (T3)args[2]!, (T4)args[3]!, (T5)args[4]!, (T6)args[5]!, (T7)args[6]!, (T8)args[7]!));
    }
}
