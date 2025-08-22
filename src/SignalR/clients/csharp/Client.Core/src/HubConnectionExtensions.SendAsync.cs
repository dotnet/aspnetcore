// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Client;

/// <summary>
/// Extension methods for <see cref="HubConnection"/>.
/// </summary>
public static partial class HubConnectionExtensions
{
    /// <summary>
    /// Invokes a hub method on the server using the specified method name.
    /// Does not wait for a response from the receiver.
    /// </summary>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous invoke.</returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static Task SendAsync(this HubConnection hubConnection, string methodName, CancellationToken cancellationToken = default)
    {
        return hubConnection.SendCoreAsync(methodName, Array.Empty<object>(), cancellationToken);
    }

    /// <summary>
    /// Invokes a hub method on the server using the specified method name and argument.
    /// Does not wait for a response from the receiver.
    /// </summary>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous invoke.</returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static Task SendAsync(this HubConnection hubConnection, string methodName, object? arg1, CancellationToken cancellationToken = default)
    {
        return hubConnection.SendCoreAsync(methodName, new[] { arg1 }, cancellationToken);
    }

    /// <summary>
    /// Invokes a hub method on the server using the specified method name and arguments.
    /// Does not wait for a response from the receiver.
    /// </summary>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous invoke.</returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static Task SendAsync(this HubConnection hubConnection, string methodName, object? arg1, object? arg2, CancellationToken cancellationToken = default)
    {
        return hubConnection.SendCoreAsync(methodName, new[] { arg1, arg2 }, cancellationToken);
    }

    /// <summary>
    /// Invokes a hub method on the server using the specified method name and arguments.
    /// Does not wait for a response from the receiver.
    /// </summary>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous invoke.</returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static Task SendAsync(this HubConnection hubConnection, string methodName, object? arg1, object? arg2, object? arg3, CancellationToken cancellationToken = default)
    {
        return hubConnection.SendCoreAsync(methodName, new[] { arg1, arg2, arg3 }, cancellationToken);
    }

    /// <summary>
    /// Invokes a hub method on the server using the specified method name and arguments.
    /// Does not wait for a response from the receiver.
    /// </summary>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous invoke.</returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static Task SendAsync(this HubConnection hubConnection, string methodName, object? arg1, object? arg2, object? arg3, object? arg4, CancellationToken cancellationToken = default)
    {
        return hubConnection.SendCoreAsync(methodName, new[] { arg1, arg2, arg3, arg4 }, cancellationToken);
    }

    /// <summary>
    /// Invokes a hub method on the server using the specified method name and arguments.
    /// Does not wait for a response from the receiver.
    /// </summary>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="arg5">The fifth argument.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous invoke.</returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static Task SendAsync(this HubConnection hubConnection, string methodName, object? arg1, object? arg2, object? arg3, object? arg4, object? arg5, CancellationToken cancellationToken = default)
    {
        return hubConnection.SendCoreAsync(methodName, new[] { arg1, arg2, arg3, arg4, arg5 }, cancellationToken);
    }

    /// <summary>
    /// Invokes a hub method on the server using the specified method name and arguments.
    /// Does not wait for a response from the receiver.
    /// </summary>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="arg5">The fifth argument.</param>
    /// <param name="arg6">The sixth argument.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous invoke.</returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static Task SendAsync(this HubConnection hubConnection, string methodName, object? arg1, object? arg2, object? arg3, object? arg4, object? arg5, object? arg6, CancellationToken cancellationToken = default)
    {
        return hubConnection.SendCoreAsync(methodName, new[] { arg1, arg2, arg3, arg4, arg5, arg6 }, cancellationToken);
    }

    /// <summary>
    /// Invokes a hub method on the server using the specified method name and arguments.
    /// Does not wait for a response from the receiver.
    /// </summary>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="arg5">The fifth argument.</param>
    /// <param name="arg6">The sixth argument.</param>
    /// <param name="arg7">The seventh argument.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous invoke.</returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static Task SendAsync(this HubConnection hubConnection, string methodName, object? arg1, object? arg2, object? arg3, object? arg4, object? arg5, object? arg6, object? arg7, CancellationToken cancellationToken = default)
    {
        return hubConnection.SendCoreAsync(methodName, new[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7 }, cancellationToken);
    }

    /// <summary>
    /// Invokes a hub method on the server using the specified method name and arguments.
    /// Does not wait for a response from the receiver.
    /// </summary>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="arg5">The fifth argument.</param>
    /// <param name="arg6">The sixth argument.</param>
    /// <param name="arg7">The seventh argument.</param>
    /// <param name="arg8">The eighth argument.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous invoke.</returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static Task SendAsync(this HubConnection hubConnection, string methodName, object? arg1, object? arg2, object? arg3, object? arg4, object? arg5, object? arg6, object? arg7, object? arg8, CancellationToken cancellationToken = default)
    {
        return hubConnection.SendCoreAsync(methodName, new[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 }, cancellationToken);
    }

    /// <summary>
    /// Invokes a hub method on the server using the specified method name and arguments.
    /// Does not wait for a response from the receiver.
    /// </summary>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="arg5">The fifth argument.</param>
    /// <param name="arg6">The sixth argument.</param>
    /// <param name="arg7">The seventh argument.</param>
    /// <param name="arg8">The eighth argument.</param>
    /// <param name="arg9">The ninth argument.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous invoke.</returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static Task SendAsync(this HubConnection hubConnection, string methodName, object? arg1, object? arg2, object? arg3, object? arg4, object? arg5, object? arg6, object? arg7, object? arg8, object? arg9, CancellationToken cancellationToken = default)
    {
        return hubConnection.SendCoreAsync(methodName, new[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 }, cancellationToken);
    }

    /// <summary>
    /// Invokes a hub method on the server using the specified method name and arguments.
    /// Does not wait for a response from the receiver.
    /// </summary>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="arg5">The fifth argument.</param>
    /// <param name="arg6">The sixth argument.</param>
    /// <param name="arg7">The seventh argument.</param>
    /// <param name="arg8">The eighth argument.</param>
    /// <param name="arg9">The ninth argument.</param>
    /// <param name="arg10">The tenth argument.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous invoke.</returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static Task SendAsync(this HubConnection hubConnection, string methodName, object? arg1, object? arg2, object? arg3, object? arg4, object? arg5, object? arg6, object? arg7, object? arg8, object? arg9, object? arg10, CancellationToken cancellationToken = default)
    {
        return hubConnection.SendCoreAsync(methodName, new[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10 }, cancellationToken);
    }
}
