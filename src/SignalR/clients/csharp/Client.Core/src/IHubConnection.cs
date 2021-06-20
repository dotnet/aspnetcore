// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Client
{
    /// <summary>
    /// A connection used to invoke hub methods on a SignalR Server.
    /// </summary>
    /// <remarks>
    /// A <see cref="HubConnection"/> should be created using <see cref="HubConnectionBuilder"/>.
    /// </remarks>
    public interface IHubConnection
    {
        /// <summary>
        /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
        /// </summary>
        /// <param name="methodName">The name of the hub method to define.</param>
        /// <param name="parameterTypes">The parameters types expected by the hub method.</param>
        /// <param name="handler">The handler that will be raised when the hub method is invoked.</param>
        /// <param name="state">A state object that will be passed to the handler.</param>
        /// <returns>A subscription that can be disposed to unsubscribe from the hub method.</returns>
        /// <remarks>
        /// This is a low level method for registering a handler. Using an <see cref="HubConnectionExtensions"/> <c>On</c> extension method is recommended.
        /// </remarks>
        IDisposable On(string methodName, Type[] parameterTypes, Func<object?[], object, Task> handler, object state);

        /// <summary>
        /// Invokes a streaming hub method on the server using the specified method name, return type and arguments.
        /// </summary>
        /// <param name="methodName">The name of the server method to invoke.</param>
        /// <param name="returnType">The return type of the server method.</param>
        /// <param name="args">The arguments used to invoke the server method.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
        /// The <see cref="Task{TResult}.Result"/> property returns a <see cref="ChannelReader{T}"/> for the streamed hub method values.
        /// </returns>
        /// <remarks>
        /// This is a low level method for invoking a streaming hub method on the server. Using an <see cref="HubConnectionExtensions"/> <c>StreamAsChannelAsync</c> extension method is recommended.
        /// </remarks>
        Task<ChannelReader<object?>> StreamAsChannelCoreAsync(string methodName, Type returnType, object?[] args, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invokes a hub method on the server using the specified method name, return type and arguments.
        /// </summary>
        /// <param name="methodName">The name of the server method to invoke.</param>
        /// <param name="returnType">The return type of the server method.</param>
        /// <param name="args">The arguments used to invoke the server method.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> that represents the asynchronous invoke.
        /// The <see cref="Task{TResult}.Result"/> property returns an <see cref="object"/> for the hub method return value.
        /// </returns>
        /// <remarks>
        /// This is a low level method for invoking a hub method on the server. Using an <see cref="HubConnectionExtensions"/> <c>InvokeAsync</c> extension method is recommended.
        /// </remarks>
        Task<object?> InvokeCoreAsync(string methodName, Type returnType, object?[] args, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invokes a hub method on the server using the specified method name and arguments.
        /// Does not wait for a response from the receiver.
        /// </summary>
        /// <param name="methodName">The name of the server method to invoke.</param>
        /// <param name="args">The arguments used to invoke the server method.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous invoke.</returns>
        /// <remarks>
        /// This is a low level method for invoking a hub method on the server. Using an <see cref="HubConnectionExtensions"/> <c>SendAsync</c> extension method is recommended.
        /// </remarks>
        Task SendCoreAsync(string methodName, object?[] args, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invokes a streaming hub method on the server using the specified method name, return type and arguments.
        /// </summary>
        /// <typeparam name="TResult">The return type of the streaming server method.</typeparam>
        /// <param name="methodName">The name of the server method to invoke.</param>
        /// <param name="args">The arguments used to invoke the server method.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>
        /// A <see cref="IAsyncEnumerable{TResult}"/> that represents the stream.
        /// </returns>
        IAsyncEnumerable<TResult> StreamAsyncCore<TResult>(string methodName, object?[] args, CancellationToken cancellationToken = default);
    }
}
