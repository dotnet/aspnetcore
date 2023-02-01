// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.SignalR.Client;

/// <summary>
/// Extension methods for <see cref="HubConnectionExtensions"/>.
/// </summary>
public static partial class HubConnectionExtensions
{
    /// <summary>
    /// Invokes a streaming hub method on the server using the specified method name and return type.
    /// </summary>
    /// <typeparam name="TResult">The return type of the streaming server method.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
    /// The <see cref="Task{TResult}.Result"/> property returns a <see cref="ChannelReader{T}"/> for the streamed hub method values.
    /// </returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static Task<ChannelReader<TResult>> StreamAsChannelAsync<TResult>(this HubConnection hubConnection, string methodName, CancellationToken cancellationToken = default)
    {
        return hubConnection.StreamAsChannelCoreAsync<TResult>(methodName, Array.Empty<object>(), cancellationToken);
    }

    /// <summary>
    /// Invokes a streaming hub method on the server using the specified method name, return type and argument.
    /// </summary>
    /// <typeparam name="TResult">The return type of the streaming server method.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
    /// The <see cref="Task{TResult}.Result"/> property returns a <see cref="ChannelReader{T}"/> for the streamed hub method values.
    /// </returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static Task<ChannelReader<TResult>> StreamAsChannelAsync<TResult>(this HubConnection hubConnection, string methodName, object? arg1, CancellationToken cancellationToken = default)
    {
        return hubConnection.StreamAsChannelCoreAsync<TResult>(methodName, new[] { arg1 }, cancellationToken);
    }

    /// <summary>
    /// Invokes a streaming hub method on the server using the specified method name, return type and arguments.
    /// </summary>
    /// <typeparam name="TResult">The return type of the streaming server method.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
    /// The <see cref="Task{TResult}.Result"/> property returns a <see cref="ChannelReader{T}"/> for the streamed hub method values.
    /// </returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static Task<ChannelReader<TResult>> StreamAsChannelAsync<TResult>(this HubConnection hubConnection, string methodName, object? arg1, object? arg2, CancellationToken cancellationToken = default)
    {
        return hubConnection.StreamAsChannelCoreAsync<TResult>(methodName, new[] { arg1, arg2 }, cancellationToken);
    }

    /// <summary>
    /// Invokes a streaming hub method on the server using the specified method name, return type and arguments.
    /// </summary>
    /// <typeparam name="TResult">The return type of the streaming server method.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
    /// The <see cref="Task{TResult}.Result"/> property returns a <see cref="ChannelReader{T}"/> for the streamed hub method values.
    /// </returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static Task<ChannelReader<TResult>> StreamAsChannelAsync<TResult>(this HubConnection hubConnection, string methodName, object? arg1, object? arg2, object? arg3, CancellationToken cancellationToken = default)
    {
        return hubConnection.StreamAsChannelCoreAsync<TResult>(methodName, new[] { arg1, arg2, arg3 }, cancellationToken);
    }

    /// <summary>
    /// Invokes a streaming hub method on the server using the specified method name, return type and arguments.
    /// </summary>
    /// <typeparam name="TResult">The return type of the streaming server method.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
    /// The <see cref="Task{TResult}.Result"/> property returns a <see cref="ChannelReader{T}"/> for the streamed hub method values.
    /// </returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static Task<ChannelReader<TResult>> StreamAsChannelAsync<TResult>(this HubConnection hubConnection, string methodName, object? arg1, object? arg2, object? arg3, object? arg4, CancellationToken cancellationToken = default)
    {
        return hubConnection.StreamAsChannelCoreAsync<TResult>(methodName, new[] { arg1, arg2, arg3, arg4 }, cancellationToken);
    }

    /// <summary>
    /// Invokes a streaming hub method on the server using the specified method name, return type and arguments.
    /// </summary>
    /// <typeparam name="TResult">The return type of the streaming server method.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="arg5">The fifth argument.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
    /// The <see cref="Task{TResult}.Result"/> property returns a <see cref="ChannelReader{T}"/> for the streamed hub method values.
    /// </returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static Task<ChannelReader<TResult>> StreamAsChannelAsync<TResult>(this HubConnection hubConnection, string methodName, object? arg1, object? arg2, object? arg3, object? arg4, object? arg5, CancellationToken cancellationToken = default)
    {
        return hubConnection.StreamAsChannelCoreAsync<TResult>(methodName, new[] { arg1, arg2, arg3, arg4, arg5 }, cancellationToken);
    }

    /// <summary>
    /// Invokes a streaming hub method on the server using the specified method name, return type and arguments.
    /// </summary>
    /// <typeparam name="TResult">The return type of the streaming server method.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="arg1">The first argument.</param>
    /// <param name="arg2">The second argument.</param>
    /// <param name="arg3">The third argument.</param>
    /// <param name="arg4">The fourth argument.</param>
    /// <param name="arg5">The fifth argument.</param>
    /// <param name="arg6">The sixth argument.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
    /// The <see cref="Task{TResult}.Result"/> property returns a <see cref="ChannelReader{T}"/> for the streamed hub method values.
    /// </returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static Task<ChannelReader<TResult>> StreamAsChannelAsync<TResult>(this HubConnection hubConnection, string methodName, object? arg1, object? arg2, object? arg3, object? arg4, object? arg5, object? arg6, CancellationToken cancellationToken = default)
    {
        return hubConnection.StreamAsChannelCoreAsync<TResult>(methodName, new[] { arg1, arg2, arg3, arg4, arg5, arg6 }, cancellationToken);
    }

    /// <summary>
    /// Invokes a streaming hub method on the server using the specified method name, return type and arguments.
    /// </summary>
    /// <typeparam name="TResult">The return type of the streaming server method.</typeparam>
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
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
    /// The <see cref="Task{TResult}.Result"/> property returns a <see cref="ChannelReader{T}"/> for the streamed hub method values.
    /// </returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static Task<ChannelReader<TResult>> StreamAsChannelAsync<TResult>(this HubConnection hubConnection, string methodName, object? arg1, object? arg2, object? arg3, object? arg4, object? arg5, object? arg6, object? arg7, CancellationToken cancellationToken = default)
    {
        return hubConnection.StreamAsChannelCoreAsync<TResult>(methodName, new[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7 }, cancellationToken);
    }

    /// <summary>
    /// Invokes a streaming hub method on the server using the specified method name, return type and arguments.
    /// </summary>
    /// <typeparam name="TResult">The return type of the streaming server method.</typeparam>
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
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
    /// The <see cref="Task{TResult}.Result"/> property returns a <see cref="ChannelReader{T}"/> for the streamed hub method values.
    /// </returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static Task<ChannelReader<TResult>> StreamAsChannelAsync<TResult>(this HubConnection hubConnection, string methodName, object? arg1, object? arg2, object? arg3, object? arg4, object? arg5, object? arg6, object? arg7, object? arg8, CancellationToken cancellationToken = default)
    {
        return hubConnection.StreamAsChannelCoreAsync<TResult>(methodName, new[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 }, cancellationToken);
    }

    /// <summary>
    /// Invokes a streaming hub method on the server using the specified method name, return type and arguments.
    /// </summary>
    /// <typeparam name="TResult">The return type of the streaming server method.</typeparam>
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
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
    /// The <see cref="Task{TResult}.Result"/> property returns a <see cref="ChannelReader{T}"/> for the streamed hub method values.
    /// </returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static Task<ChannelReader<TResult>> StreamAsChannelAsync<TResult>(this HubConnection hubConnection, string methodName, object? arg1, object? arg2, object? arg3, object? arg4, object? arg5, object? arg6, object? arg7, object? arg8, object? arg9, CancellationToken cancellationToken = default)
    {
        return hubConnection.StreamAsChannelCoreAsync<TResult>(methodName, new[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 }, cancellationToken);
    }

    /// <summary>
    /// Invokes a streaming hub method on the server using the specified method name, return type and arguments.
    /// </summary>
    /// <typeparam name="TResult">The return type of the streaming server method.</typeparam>
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
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
    /// The <see cref="Task{TResult}.Result"/> property returns a <see cref="ChannelReader{T}"/> for the streamed hub method values.
    /// </returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static Task<ChannelReader<TResult>> StreamAsChannelAsync<TResult>(this HubConnection hubConnection, string methodName, object? arg1, object? arg2, object? arg3, object? arg4, object? arg5, object? arg6, object? arg7, object? arg8, object? arg9, object? arg10, CancellationToken cancellationToken = default)
    {
        return hubConnection.StreamAsChannelCoreAsync<TResult>(methodName, new[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10 }, cancellationToken);
    }

    /// <summary>
    /// Invokes a streaming hub method on the server using the specified method name, return type and arguments.
    /// </summary>
    /// <typeparam name="TResult">The return type of the streaming server method.</typeparam>
    /// <param name="hubConnection">The hub connection.</param>
    /// <param name="methodName">The name of the server method to invoke.</param>
    /// <param name="args">The arguments used to invoke the server method.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
    /// The <see cref="Task{TResult}.Result"/> property returns a <see cref="ChannelReader{T}"/> for the streamed hub method values.
    /// </returns>
    public static async Task<ChannelReader<TResult>> StreamAsChannelCoreAsync<TResult>(this HubConnection hubConnection, string methodName, object?[] args, CancellationToken cancellationToken = default)
    {
        ArgumentNullThrowHelper.ThrowIfNull(hubConnection);

        var inputChannel = await hubConnection.StreamAsChannelCoreAsync(methodName, typeof(TResult), args, cancellationToken).ConfigureAwait(false);
        var outputChannel = Channel.CreateUnbounded<TResult>();

        // Intentionally avoid passing the CancellationToken to RunChannel. The token is only meant to cancel the intial setup, not the enumeration.
        _ = RunChannel(inputChannel, outputChannel);

        return outputChannel.Reader;
    }

    // Function to provide a way to run async code as fire-and-forget
    // The output channel is how we signal completion to the caller.
    private static async Task RunChannel<TResult>(ChannelReader<object?> inputChannel, Channel<TResult> outputChannel)
    {
        try
        {
            while (await inputChannel.WaitToReadAsync().ConfigureAwait(false))
            {
                while (inputChannel.TryRead(out var item))
                {
                    while (!outputChannel.Writer.TryWrite((TResult)item!))
                    {
                        if (!await outputChannel.Writer.WaitToWriteAsync().ConfigureAwait(false))
                        {
                            // Failed to write to the output channel because it was closed. Nothing really we can do but abort here.
                            return;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            outputChannel.Writer.TryComplete(ex);
        }
        finally
        {
            // This will safely no-op if the catch block above ran.
            outputChannel.Writer.TryComplete();

            // Needed to avoid UnobservedTaskExceptions
            _ = inputChannel.Completion.Exception;
        }
    }
}
