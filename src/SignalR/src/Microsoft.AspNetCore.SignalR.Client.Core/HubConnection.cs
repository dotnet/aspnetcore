// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Client.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Client
{
    /// <summary>
    /// A connection used to invoke hub methods on a SignalR Server.
    /// </summary>
    /// <remarks>
    /// A <see cref="HubConnection"/> should be created using <see cref="HubConnectionBuilder"/>.
    /// Before hub methods can be invoked the connection must be started using <see cref="StartAsync"/>.
    /// Clean up a connection using <see cref="StopAsync"/> or <see cref="DisposeAsync"/>.
    /// </remarks>
    public partial class HubConnection
    {
        public static readonly TimeSpan DefaultServerTimeout = TimeSpan.FromSeconds(30); // Server ping rate is 15 sec, this is 2 times that.
        public static readonly TimeSpan DefaultHandshakeTimeout = TimeSpan.FromSeconds(15);
        public static readonly TimeSpan DefaultKeepAliveInterval = TimeSpan.FromSeconds(15);

        // This lock protects the connection state.
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);

        private static readonly MethodInfo _sendStreamItemsMethod = typeof(HubConnection).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Single(m => m.Name.Equals("SendStreamItems"));

        // Persistent across all connections
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly IHubProtocol _protocol;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConnectionFactory _connectionFactory;
        private readonly ConcurrentDictionary<string, InvocationHandlerList> _handlers = new ConcurrentDictionary<string, InvocationHandlerList>(StringComparer.Ordinal);

        private long _nextActivationServerTimeout;
        private long _nextActivationSendPing;
        private bool _disposed;
        private bool _hasInherentKeepAlive;

        private CancellationToken _uploadStreamToken;

        private readonly ConnectionLogScope _logScope;

        // Transient state to a connection
        private ConnectionState _connectionState;
        private int _serverProtocolMinorVersion;

        public event Func<Exception, Task> Closed;

        // internal for testing purposes
        internal TimeSpan TickRate { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the server timeout interval for the connection. 
        /// </summary>
        /// <remarks>
        /// The client times out if it hasn't heard from the server for `this` long.
        /// </remarks>
        public TimeSpan ServerTimeout { get; set; } = DefaultServerTimeout;

        /// <summary>
        /// Gets or sets the interval at which the client sends ping messages.
        /// </summary>
        /// <remarks>
        /// Sending any message resets the timer to the start of the interval.
        /// </remarks>
        public TimeSpan KeepAliveInterval { get; set; } = DefaultKeepAliveInterval;

        /// <summary>
        /// Gets or sets the timeout for the initial handshake.
        /// </summary>
        public TimeSpan HandshakeTimeout { get; set; } = DefaultHandshakeTimeout;

        /// <summary>
        /// Indicates the state of the <see cref="HubConnection"/> to the server.
        /// </summary>
        public HubConnectionState State
        {
            get
            {
                // Copy reference for thread-safety
                var connectionState = _connectionState;
                if (connectionState == null || connectionState.Stopped)
                {
                    return HubConnectionState.Disconnected;
                }

                return HubConnectionState.Connected;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HubConnection"/> class.
        /// </summary>
        /// <param name="connectionFactory">The <see cref="IConnectionFactory" /> used to create a connection each time <see cref="StartAsync" /> is called.</param>
        /// <param name="protocol">The <see cref="IHubProtocol" /> used by the connection.</param>
        /// <param name="serviceProvider">An <see cref="IServiceProvider"/> containing the services provided to this <see cref="HubConnection"/> instance.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <remarks>
        /// The <see cref="IServiceProvider"/> used to initialize the connection will be disposed when the connection is disposed.
        /// </remarks>
        public HubConnection(IConnectionFactory connectionFactory, IHubProtocol protocol, IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
            : this(connectionFactory, protocol, loggerFactory)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HubConnection"/> class.
        /// </summary>
        /// <param name="connectionFactory">The <see cref="IConnectionFactory" /> used to create a connection each time <see cref="StartAsync" /> is called.</param>
        /// <param name="protocol">The <see cref="IHubProtocol" /> used by the connection.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public HubConnection(IConnectionFactory connectionFactory, IHubProtocol protocol, ILoggerFactory loggerFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));

            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            _logger = _loggerFactory.CreateLogger<HubConnection>();

            _logScope = new ConnectionLogScope();
        }

        /// <summary>
        /// Starts a connection to the server.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous start.</returns>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            CheckDisposed();
            using (_logger.BeginScope(_logScope))
            {
                await StartAsyncCore(cancellationToken).ForceAsync();
            }
        }

        /// <summary>
        /// Stops a connection to the server.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous stop.</returns>
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            CheckDisposed();
            using (_logger.BeginScope(_logScope))
            {
                await StopAsyncCore(disposing: false).ForceAsync();
            }
        }

        // Current plan for IAsyncDisposable is that DisposeAsync will NOT take a CancellationToken
        // https://github.com/dotnet/csharplang/blob/195efa07806284d7b57550e7447dc8bd39c156bf/proposals/async-streams.md#iasyncdisposable
        /// <summary>
        /// Disposes the <see cref="HubConnection"/>.
        /// </summary>
        /// <returns>A <see cref="Task"/> that represents the asynchronous dispose.</returns>
        public async Task DisposeAsync()
        {
            if (!_disposed)
            {
                using (_logger.BeginScope(_logScope))
                {
                    await StopAsyncCore(disposing: true).ForceAsync();
                }
            }
        }

        // If the registered callback blocks it can cause the client to stop receiving messages. If you need to block, get off the current thread first.
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
        public IDisposable On(string methodName, Type[] parameterTypes, Func<object[], object, Task> handler, object state)
        {
            Log.RegisteringHandler(_logger, methodName);

            CheckDisposed();

            // It's OK to be disposed while registering a callback, we'll just never call the callback anyway (as with all the callbacks registered before disposal).
            var invocationHandler = new InvocationHandler(parameterTypes, handler, state);
            var invocationList = _handlers.AddOrUpdate(methodName, _ => new InvocationHandlerList(invocationHandler),
                (_, invocations) =>
                {
                    lock (invocations)
                    {
                        invocations.Add(invocationHandler);
                    }
                    return invocations;
                });

            return new Subscription(invocationHandler, invocationList);
        }

        /// <summary>
        /// Removes all handlers associated with the method with the specified method name.
        /// </summary>
        /// <param name="methodName">The name of the hub method from which handlers are being removed</param>
        public void Remove(string methodName)
        {
            CheckDisposed();
            Log.RemovingHandlers(_logger, methodName);
            _handlers.TryRemove(methodName, out _);
        }

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
        public async Task<ChannelReader<object>> StreamAsChannelCoreAsync(string methodName, Type returnType, object[] args, CancellationToken cancellationToken = default)
        {
            using (_logger.BeginScope(_logScope))
            {
                return await StreamAsChannelCoreAsyncCore(methodName, returnType, args, cancellationToken).ForceAsync();
            }
        }

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
        public async Task<object> InvokeCoreAsync(string methodName, Type returnType, object[] args, CancellationToken cancellationToken = default)
        {
            using (_logger.BeginScope(_logScope))
            {
                return await InvokeCoreAsyncCore(methodName, returnType, args, cancellationToken).ForceAsync();
            }
        }

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
        public async Task SendCoreAsync(string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            using (_logger.BeginScope(_logScope))
            {
                await SendCoreAsyncCore(methodName, args, cancellationToken).ForceAsync();
            }
        }

        private async Task StartAsyncCore(CancellationToken cancellationToken)
        {
            await WaitConnectionLockAsync();
            try
            {
                if (_connectionState != null)
                {
                    // We're already connected
                    return;
                }

                cancellationToken.ThrowIfCancellationRequested();

                CheckDisposed();

                Log.Starting(_logger);

                // Start the connection
                var connection = await _connectionFactory.ConnectAsync(_protocol.TransferFormat);
                var startingConnectionState = new ConnectionState(connection, this);
                _hasInherentKeepAlive = connection.Features.Get<IConnectionInherentKeepAliveFeature>()?.HasInherentKeepAlive ?? false;

                // From here on, if an error occurs we need to shut down the connection because
                // we still own it.
                try
                {
                    Log.HubProtocol(_logger, _protocol.Name, _protocol.Version);
                    await HandshakeAsync(startingConnectionState, cancellationToken);
                }
                catch (Exception ex)
                {
                    Log.ErrorStartingConnection(_logger, ex);

                    // Can't have any invocations to cancel, we're in the lock.
                    await CloseAsync(startingConnectionState.Connection);
                    throw;
                }

                // Set this at the end to avoid setting internal state until the connection is real
                _connectionState = startingConnectionState;
                _connectionState.ReceiveTask = ReceiveLoop(_connectionState);

                Log.Started(_logger);
            }
            finally
            {
                ReleaseConnectionLock();
            }
        }

        private Task CloseAsync(ConnectionContext connection)
        {
            return _connectionFactory.DisposeAsync(connection);
        }

        // This method does both Dispose and Start, the 'disposing' flag indicates which.
        // The behaviors are nearly identical, except that the _disposed flag is set in the lock
        // if we're disposing.
        private async Task StopAsyncCore(bool disposing)
        {
            // Block a Start from happening until we've finished capturing the connection state.
            ConnectionState connectionState;
            await WaitConnectionLockAsync();
            try
            {
                if (disposing && _disposed)
                {
                    // DisposeAsync should be idempotent.
                    return;
                }

                CheckDisposed();
                connectionState = _connectionState;

                // Set the stopping flag so that any invocations after this get a useful error message instead of
                // silently failing or throwing an error about the pipe being completed.
                if (connectionState != null)
                {
                    connectionState.Stopping = true;
                }

                if (disposing)
                {
                    (_serviceProvider as IDisposable)?.Dispose();
                    _disposed = true;
                }
            }
            finally
            {
                ReleaseConnectionLock();
            }

            // Now stop the connection we captured
            if (connectionState != null)
            {
                await connectionState.StopAsync();
            }
        }

        private async Task<ChannelReader<object>> StreamAsChannelCoreAsyncCore(string methodName, Type returnType, object[] args, CancellationToken cancellationToken)
        {
            async Task OnStreamCanceled(InvocationRequest irq)
            {
                // We need to take the connection lock in order to ensure we a) have a connection and b) are the only one accessing the write end of the pipe.
                await WaitConnectionLockAsync();
                try
                {
                    if (_connectionState != null)
                    {
                        Log.SendingCancellation(_logger, irq.InvocationId);

                        // Fire and forget, if it fails that means we aren't connected anymore.
                        _ = SendHubMessage(new CancelInvocationMessage(irq.InvocationId), irq.CancellationToken);
                    }
                    else
                    {
                        Log.UnableToSendCancellation(_logger, irq.InvocationId);
                    }
                }
                finally
                {
                    ReleaseConnectionLock();
                }

                // Cancel the invocation
                irq.Dispose();
            }

            var readers = PackageStreamingParams(args);

            CheckDisposed();
            await WaitConnectionLockAsync();

            ChannelReader<object> channel;
            try
            {
                CheckDisposed();
                CheckConnectionActive(nameof(StreamAsChannelCoreAsync));
                cancellationToken.ThrowIfCancellationRequested();

                // I just want an excuse to use 'irq' as a variable name...
                var irq = InvocationRequest.Stream(cancellationToken, returnType, _connectionState.GetNextId(), _loggerFactory, this, out channel);
                await InvokeStreamCore(methodName, irq, args, cancellationToken);

                if (cancellationToken.CanBeCanceled)
                {
                    cancellationToken.Register(state => _ = OnStreamCanceled((InvocationRequest)state), irq);
                }
            }
            finally
            {
                ReleaseConnectionLock();
            }

            LaunchStreams(readers, cancellationToken);

            return channel;
        }

        private Dictionary<string, object> PackageStreamingParams(object[] args)
        {
            // lazy initialized, to avoid allocating unecessary dictionaries
            Dictionary<string, object> readers = null;

            for (var i = 0; i < args.Length; i++)
            {
                if (ReflectionHelper.IsStreamingType(args[i].GetType()))
                {
                    if (readers == null)
                    {
                        readers = new Dictionary<string, object>();
                    }

                    var id = _connectionState.GetNextStreamId();
                    readers[id] = args[i];
                    args[i] = new StreamPlaceholder(id);

                    Log.StartingStream(_logger, id);
                }
            }

            return readers;
        }

        private void LaunchStreams(Dictionary<string, object> readers, CancellationToken cancellationToken)
        {
            if (readers == null)
            {
                // if there were no streaming parameters then readers is never initialized
                return;
            }
            foreach (var kvp in readers)
            {
                var reader = kvp.Value;

                // For each stream that needs to be sent, run a "send items" task in the background.
                // This reads from the channel, attaches streamId, and sends to server.
                // A single background thread here quickly gets messy.
                _ = _sendStreamItemsMethod
                    .MakeGenericMethod(reader.GetType().GetGenericArguments())
                    .Invoke(this, new object[] { kvp.Key.ToString(), reader, cancellationToken });
            }
        }

        // this is called via reflection using the `_sendStreamItems` field 
        private async Task SendStreamItems<T>(string streamId, ChannelReader<T> reader, CancellationToken token)
        {
            Log.StartingStream(_logger, streamId);

            var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(_uploadStreamToken, token).Token;

            string responseError = null;
            try
            {
                while (await reader.WaitToReadAsync(combinedToken))
                {
                    while (!combinedToken.IsCancellationRequested && reader.TryRead(out var item))
                    {
                        await SendWithLock(new StreamDataMessage(streamId, item));
                        Log.SendingStreamItem(_logger, streamId);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Log.CancelingStream(_logger, streamId);
                responseError = $"Stream canceled by client.";
            }

            Log.CompletingStream(_logger, streamId);
            await SendWithLock(new StreamCompleteMessage(streamId, responseError));
        }

        private async Task<object> InvokeCoreAsyncCore(string methodName, Type returnType, object[] args, CancellationToken cancellationToken)
        {
            var readers = PackageStreamingParams(args);

            CheckDisposed();
            await WaitConnectionLockAsync();

            Task<object> invocationTask;
            try
            {
                CheckDisposed();
                CheckConnectionActive(nameof(InvokeCoreAsync));

                var irq = InvocationRequest.Invoke(cancellationToken, returnType, _connectionState.GetNextId(), _loggerFactory, this, out invocationTask);
                await InvokeCore(methodName, irq, args, cancellationToken);
            }
            finally
            {
                ReleaseConnectionLock();
            }

            LaunchStreams(readers, cancellationToken);

            // Wait for this outside the lock, because it won't complete until the server responds
            return await invocationTask;
        }

        private async Task InvokeCore(string methodName, InvocationRequest irq, object[] args, CancellationToken cancellationToken)
        {
            Log.PreparingBlockingInvocation(_logger, irq.InvocationId, methodName, irq.ResultType.FullName, args.Length);

            // Client invocations are always blocking
            var invocationMessage = new InvocationMessage(irq.InvocationId, methodName, args);

            Log.RegisteringInvocation(_logger, invocationMessage.InvocationId);
            _connectionState.AddInvocation(irq);

            // Trace the full invocation
            Log.IssuingInvocation(_logger, invocationMessage.InvocationId, irq.ResultType.FullName, methodName, args);

            try
            {
                await SendHubMessage(invocationMessage, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.FailedToSendInvocation(_logger, invocationMessage.InvocationId, ex);
                _connectionState.TryRemoveInvocation(invocationMessage.InvocationId, out _);
                irq.Fail(ex);
            }
        }

        private async Task InvokeStreamCore(string methodName, InvocationRequest irq, object[] args, CancellationToken cancellationToken)
        {
            AssertConnectionValid();

            Log.PreparingStreamingInvocation(_logger, irq.InvocationId, methodName, irq.ResultType.FullName, args.Length);

            var invocationMessage = new StreamInvocationMessage(irq.InvocationId, methodName, args);

            Log.RegisteringInvocation(_logger, invocationMessage.InvocationId);

            _connectionState.AddInvocation(irq);

            // Trace the full invocation
            Log.IssuingInvocation(_logger, invocationMessage.InvocationId, irq.ResultType.FullName, methodName, args);

            try
            {
                await SendHubMessage(invocationMessage, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.FailedToSendInvocation(_logger, invocationMessage.InvocationId, ex);
                _connectionState.TryRemoveInvocation(invocationMessage.InvocationId, out _);
                irq.Fail(ex);
            }
        }

        private async Task SendHubMessage(HubMessage hubMessage, CancellationToken cancellationToken = default)
        {
            AssertConnectionValid();

            _protocol.WriteMessage(hubMessage, _connectionState.Connection.Transport.Output);

            Log.SendingMessage(_logger, hubMessage);

            // REVIEW: If a token is passed in and is canceled during FlushAsync it seems to break .Complete()...
            await _connectionState.Connection.Transport.Output.FlushAsync();
            Log.MessageSent(_logger, hubMessage);

            // We've sent a message, so don't ping for a while
            ResetSendPing();
        }

        private async Task SendCoreAsyncCore(string methodName, object[] args, CancellationToken cancellationToken)
        {
            var readers = PackageStreamingParams(args);

            Log.PreparingNonBlockingInvocation(_logger, methodName, args.Length);
            var invocationMessage = new InvocationMessage(null, methodName, args);
            await SendWithLock(invocationMessage, callerName: nameof(SendCoreAsync));

            LaunchStreams(readers, cancellationToken);
        }

        private async Task SendWithLock(HubMessage message, CancellationToken cancellationToken = default, [CallerMemberName] string callerName = "")
        {
            CheckDisposed();
            await WaitConnectionLockAsync();
            try
            {
                CheckConnectionActive(callerName);
                CheckDisposed();
                await SendHubMessage(message, cancellationToken);
            }
            finally
            {
                ReleaseConnectionLock();
            }
        }

        private async Task<(bool close, Exception exception)> ProcessMessagesAsync(HubMessage message, ConnectionState connectionState)
        {
            Log.ResettingKeepAliveTimer(_logger);
            ResetTimeout();

            InvocationRequest irq;
            switch (message)
            {
                case InvocationBindingFailureMessage bindingFailure:
                    // The server can't receive a response, so we just drop the message and log
                    // REVIEW: Is this the right approach?
                    Log.ArgumentBindingFailure(_logger, bindingFailure.InvocationId, bindingFailure.Target, bindingFailure.BindingFailure.SourceException);
                    break;
                case InvocationMessage invocation:
                    Log.ReceivedInvocation(_logger, invocation.InvocationId, invocation.Target, invocation.Arguments);
                    await DispatchInvocationAsync(invocation);
                    break;
                case CompletionMessage completion:
                    if (!connectionState.TryRemoveInvocation(completion.InvocationId, out irq))
                    {
                        Log.DroppedCompletionMessage(_logger, completion.InvocationId);
                        break;
                    }

                    DispatchInvocationCompletion(completion, irq);
                    irq.Dispose();

                    break;
                case StreamItemMessage streamItem:
                    // if there's no open StreamInvocation with the given id, then complete with an error
                    if (!connectionState.TryGetInvocation(streamItem.InvocationId, out irq))
                    {
                        Log.DroppedStreamMessage(_logger, streamItem.InvocationId);
                        return (close: false, exception: null);
                    }
                    await DispatchInvocationStreamItemAsync(streamItem, irq);
                    break;
                case CloseMessage close:
                    if (string.IsNullOrEmpty(close.Error))
                    {
                        Log.ReceivedClose(_logger);
                        return (close: true, exception: null);
                    }
                    else
                    {
                        Log.ReceivedCloseWithError(_logger, close.Error);
                        return (close: true, exception: new HubException($"The server closed the connection with the following error: {close.Error}"));
                    }
                case PingMessage _:
                    Log.ReceivedPing(_logger);
                    // timeout is reset above, on receiving any message
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected message type: {message.GetType().FullName}");
            }

            return (close: false, exception: null);
        }

        private async Task DispatchInvocationAsync(InvocationMessage invocation)
        {
            // Find the handler
            if (!_handlers.TryGetValue(invocation.Target, out var invocationHandlerList))
            {
                Log.MissingHandler(_logger, invocation.Target);
                return;
            }

            // Grabbing the current handlers
            var copiedHandlers = invocationHandlerList.GetHandlers();
            foreach (var handler in copiedHandlers)
            {
                try
                {
                    await handler.InvokeAsync(invocation.Arguments);
                }
                catch (Exception ex)
                {
                    Log.ErrorInvokingClientSideMethod(_logger, invocation.Target, ex);
                }
            }
        }

        private async Task DispatchInvocationStreamItemAsync(StreamItemMessage streamItem, InvocationRequest irq)
        {
            Log.ReceivedStreamItem(_logger, streamItem.InvocationId);

            if (irq.CancellationToken.IsCancellationRequested)
            {
                Log.CancelingStreamItem(_logger, irq.InvocationId);
            }
            else if (!await irq.StreamItem(streamItem.Item))
            {
                Log.ReceivedStreamItemAfterClose(_logger, irq.InvocationId);
            }
        }

        private void DispatchInvocationCompletion(CompletionMessage completion, InvocationRequest irq)
        {
            Log.ReceivedInvocationCompletion(_logger, completion.InvocationId);

            if (irq.CancellationToken.IsCancellationRequested)
            {
                Log.CancelingInvocationCompletion(_logger, irq.InvocationId);
            }
            else
            {
                irq.Complete(completion);
            }
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HubConnection));
            }
        }

        private async Task HandshakeAsync(ConnectionState startingConnectionState, CancellationToken cancellationToken)
        {
            // Send the Handshake request
            Log.SendingHubHandshake(_logger);

            var handshakeRequest = new HandshakeRequestMessage(_protocol.Name, _protocol.Version);
            HandshakeProtocol.WriteRequestMessage(handshakeRequest, startingConnectionState.Connection.Transport.Output);

            var sendHandshakeResult = await startingConnectionState.Connection.Transport.Output.FlushAsync(CancellationToken.None);

            if (sendHandshakeResult.IsCompleted)
            {
                // The other side disconnected
                throw new InvalidOperationException("The server disconnected before the handshake was completed");
            }

            try
            {
                using (var handshakeCts = new CancellationTokenSource(HandshakeTimeout))
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, handshakeCts.Token))
                {
                    while (true)
                    {
                        var result = await startingConnectionState.Connection.Transport.Input.ReadAsync(cts.Token);

                        var buffer = result.Buffer;
                        var consumed = buffer.Start;
                        var examined = buffer.End;

                        try
                        {
                            // Read first message out of the incoming data
                            if (!buffer.IsEmpty)
                            {
                                if (HandshakeProtocol.TryParseResponseMessage(ref buffer, out var message))
                                {
                                    // Adjust consumed and examined to point to the end of the handshake
                                    // response, this handles the case where invocations are sent in the same payload
                                    // as the negotiate response.
                                    consumed = buffer.Start;
                                    examined = consumed;

                                    if (message.Error != null)
                                    {
                                        Log.HandshakeServerError(_logger, message.Error);
                                        throw new HubException(
                                            $"Unable to complete handshake with the server due to an error: {message.Error}");
                                    }

                                    _serverProtocolMinorVersion = message.MinorVersion;

                                    break;
                                }
                            }

                            if (result.IsCompleted)
                            {
                                // Not enough data, and we won't be getting any more data.
                                throw new InvalidOperationException(
                                    "The server disconnected before sending a handshake response");
                            }
                        }
                        finally
                        {
                            startingConnectionState.Connection.Transport.Input.AdvanceTo(consumed, examined);
                        }
                    }
                }
            }
            
            // shutdown if we're unable to read handshake
            // Ignore HubException because we throw it when we receive a handshake response with an error
            // And because we already have the error, we don't need to log that the handshake failed
            catch (Exception ex) when (!(ex is HubException))
            {
                Log.ErrorReceivingHandshakeResponse(_logger, ex);
                throw;
            }

            Log.HandshakeComplete(_logger);
        }

        private async Task ReceiveLoop(ConnectionState connectionState)
        {
            // We hold a local capture of the connection state because StopAsync may dump out the current one.
            // We'll be locking any time we want to check back in to the "active" connection state.

            Log.ReceiveLoopStarting(_logger);

            // Performs periodic tasks -- here sending pings and checking timeout
            // Disposed with `timer.Stop()` in the finally block below
            var timer = new TimerAwaitable(TickRate, TickRate);
            _ = TimerLoop(timer);

            var uploadStreamSource = new CancellationTokenSource();
            _uploadStreamToken = uploadStreamSource.Token;

            try
            {
                while (true)
                {
                    var result = await connectionState.Connection.Transport.Input.ReadAsync();
                    var buffer = result.Buffer;

                    try
                    {
                        if (result.IsCanceled)
                        {
                            // We were canceled. Possibly because we were stopped gracefully
                            break;
                        }
                        else if (!buffer.IsEmpty)
                        {
                            Log.ProcessingMessage(_logger, buffer.Length);

                            var close = false;

                            while (_protocol.TryParseMessage(ref buffer, connectionState, out var message))
                            {
                                Exception exception;

                                // We have data, process it
                                (close, exception) = await ProcessMessagesAsync(message, connectionState);
                                if (close)
                                {
                                    // Closing because we got a close frame, possibly with an error in it.
                                    connectionState.CloseException = exception;
                                    break;
                                }
                            }

                            // If we're closing stop everything
                            if (close)
                            {
                                break;
                            }
                        }

                        if (result.IsCompleted)
                        {
                            if (!buffer.IsEmpty)
                            {
                                throw new InvalidDataException("Connection terminated while reading a message.");
                            }
                            break;
                        }
                    }
                    finally
                    {
                        // The buffer was sliced up to where it was consumed, so we can just advance to the start.
                        // We mark examined as `buffer.End` so that if we didn't receive a full frame, we'll wait for more data
                        // before yielding the read again.
                        connectionState.Connection.Transport.Input.AdvanceTo(buffer.Start, buffer.End);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.ServerDisconnectedWithError(_logger, ex);
                connectionState.CloseException = ex;
            }
            finally
            {
                timer.Stop();
                uploadStreamSource.Cancel();
            }

            // Clear the connectionState field
            await WaitConnectionLockAsync();
            try
            {
                SafeAssert(ReferenceEquals(_connectionState, connectionState),
                    "Someone other than ReceiveLoop cleared the connection state!");
                _connectionState = null;
            }
            finally
            {
                ReleaseConnectionLock();
            }

            // Dispose the connection
            await CloseAsync(connectionState.Connection);

            // Cancel any outstanding invocations within the connection lock
            connectionState.CancelOutstandingInvocations(connectionState.CloseException);

            if (connectionState.CloseException != null)
            {
                Log.ShutdownWithError(_logger, connectionState.CloseException);
            }
            else
            {
                Log.ShutdownConnection(_logger);
            }

            var closed = Closed;

            // There is no need to start a new task if there is no Closed event registered
            if (closed != null)
            {
                // Fire-and-forget the closed event
                _ = RunClosedEvent(closed, connectionState.CloseException);
            }
        }

        public void ResetSendPing()
        {
            Volatile.Write(ref _nextActivationSendPing, (DateTime.UtcNow + KeepAliveInterval).Ticks);
        }

        public void ResetTimeout()
        {
            Volatile.Write(ref _nextActivationServerTimeout, (DateTime.UtcNow + ServerTimeout).Ticks);
        }

        private async Task TimerLoop(TimerAwaitable timer)
        {
            // Tell the server we intend to ping
            // Old clients never ping, and shouldn't be timed out
            // So ping to tell the server that we should be timed out if we stop
            await SendHubMessage(PingMessage.Instance);

            // initialize the timers
            timer.Start();
            ResetTimeout();
            ResetSendPing();

            using (timer)
            {
                // await returns True until `timer.Stop()` is called in the `finally` block of `ReceiveLoop`
                while (await timer)
                {
                    await RunTimerActions();
                }
            }
        }

        // Internal for testing
        internal async Task RunTimerActions()
        {
            if (!_hasInherentKeepAlive && DateTime.UtcNow.Ticks > Volatile.Read(ref _nextActivationServerTimeout))
            {
                OnServerTimeout();
            }

            if (DateTime.UtcNow.Ticks > Volatile.Read(ref _nextActivationSendPing))
            {
                await PingServer();
            }
        }

        private void OnServerTimeout()
        {
            _connectionState.CloseException = new TimeoutException(
                $"Server timeout ({ServerTimeout.TotalMilliseconds:0.00}ms) elapsed without receiving a message from the server.");
            _connectionState.Connection.Transport.Input.CancelPendingRead();
        }

        private async Task PingServer()
        {
            if (_disposed || !_connectionLock.Wait(0))
            {
                Log.UnableToAcquireConnectionLockForPing(_logger);
                return;
            }

            Log.AcquiredConnectionLockForPing(_logger);

            try
            {
                if (_disposed || _connectionState == null || _connectionState.Stopping)
                {
                    return;
                }
                await SendHubMessage(PingMessage.Instance);
            }
            finally
            {
                ReleaseConnectionLock();
            }
        }

        private async Task RunClosedEvent(Func<Exception, Task> closed, Exception closeException)
        {
            // Dispatch to the thread pool before we invoke the user callback
            await AwaitableThreadPool.Yield();

            try
            {
                Log.InvokingClosedEventHandler(_logger);
                await closed.Invoke(closeException);
            }
            catch (Exception ex)
            {
                Log.ErrorDuringClosedEvent(_logger, ex);
            }
        }

        private void CheckConnectionActive(string methodName)
        {
            if (_connectionState == null || _connectionState.Stopping)
            {
                throw new InvalidOperationException($"The '{methodName}' method cannot be called if the connection is not active");
            }
        }

        // Debug.Assert plays havoc with Unit Tests. But I want something that I can "assert" only in Debug builds.
        [Conditional("DEBUG")]
        private static void SafeAssert(bool condition, string message, [CallerMemberName] string memberName = null, [CallerFilePath] string fileName = null, [CallerLineNumber] int lineNumber = 0)
        {
            if (!condition)
            {
                throw new Exception($"Assertion failed in {memberName}, at {fileName}:{lineNumber}: {message}");
            }
        }

        [Conditional("DEBUG")]
        private void AssertInConnectionLock([CallerMemberName] string memberName = null, [CallerFilePath] string fileName = null, [CallerLineNumber] int lineNumber = 0) => SafeAssert(_connectionLock.CurrentCount == 0, "We're not in the Connection Lock!", memberName, fileName, lineNumber);

        [Conditional("DEBUG")]
        private void AssertConnectionValid([CallerMemberName] string memberName = null, [CallerFilePath] string fileName = null, [CallerLineNumber] int lineNumber = 0)
        {
            AssertInConnectionLock(memberName, fileName, lineNumber);
            SafeAssert(_connectionState != null, "We don't have a connection!", memberName, fileName, lineNumber);
        }

        private Task WaitConnectionLockAsync([CallerMemberName] string memberName = null, [CallerFilePath] string filePath = null, [CallerLineNumber] int lineNumber = 0)
        {
            Log.WaitingOnConnectionLock(_logger, memberName, filePath, lineNumber);
            return _connectionLock.WaitAsync();
        }

        private void ReleaseConnectionLock([CallerMemberName] string memberName = null,
            [CallerFilePath] string filePath = null, [CallerLineNumber] int lineNumber = 0)
        {
            Log.ReleasingConnectionLock(_logger, memberName, filePath, lineNumber);
            _connectionLock.Release();
        }

        private class Subscription : IDisposable
        {
            private readonly InvocationHandler _handler;
            private readonly InvocationHandlerList _handlerList;

            public Subscription(InvocationHandler handler, InvocationHandlerList handlerList)
            {
                _handler = handler;
                _handlerList = handlerList;
            }

            public void Dispose()
            {
                _handlerList.Remove(_handler);
            }
        }

        private class InvocationHandlerList
        {
            private readonly List<InvocationHandler> _invocationHandlers;
            // A lazy cached copy of the handlers that doesn't change for thread safety. 
            // Adding or removing a handler sets this to null.
            private InvocationHandler[] _copiedHandlers;

            internal InvocationHandlerList(InvocationHandler handler)
            {
                _invocationHandlers = new List<InvocationHandler>() { handler };
            }

            internal InvocationHandler[] GetHandlers()
            {
                var handlers = _copiedHandlers;
                if (handlers == null)
                {
                    lock (_invocationHandlers)
                    {
                        // Check if the handlers are set, if not we'll copy them over.
                        if (_copiedHandlers == null)
                        {
                            _copiedHandlers = _invocationHandlers.ToArray();
                        }
                        handlers = _copiedHandlers;
                    }
                }
                return handlers;
            }

            internal void Add(InvocationHandler handler)
            {
                lock (_invocationHandlers)
                {
                    _invocationHandlers.Add(handler);
                    _copiedHandlers = null;
                }
            }

            internal void Remove(InvocationHandler handler)
            {
                lock (_invocationHandlers)
                {
                    if (_invocationHandlers.Remove(handler))
                    {
                        _copiedHandlers = null;
                    }
                }
            }
        }

        private readonly struct InvocationHandler
        {
            public Type[] ParameterTypes { get; }
            private readonly Func<object[], object, Task> _callback;
            private readonly object _state;

            public InvocationHandler(Type[] parameterTypes, Func<object[], object, Task> callback, object state)
            {
                _callback = callback;
                ParameterTypes = parameterTypes;
                _state = state;
            }

            public Task InvokeAsync(object[] parameters)
            {
                return _callback(parameters, _state);
            }
        }

        // Represents all the transient state about a connection
        // This includes binding information because return type binding depends upon _pendingCalls
        private class ConnectionState : IInvocationBinder
        {
            private volatile bool _stopping;
            private readonly HubConnection _hubConnection;

            private TaskCompletionSource<object> _stopTcs;
            private readonly object _lock = new object();
            private readonly Dictionary<string, InvocationRequest> _pendingCalls = new Dictionary<string, InvocationRequest>(StringComparer.Ordinal);
            private int _nextInvocationId;
            private int _nextStreamId;

            public ConnectionContext Connection { get; }
            public Task ReceiveTask { get; set; }
            public Exception CloseException { get; set; }

            public bool Stopping
            {
                get => _stopping;
                set => _stopping = value;
            }

            public bool Stopped => _stopTcs?.Task.Status == TaskStatus.RanToCompletion;

            public ConnectionState(ConnectionContext connection, HubConnection hubConnection)
            {
                _hubConnection = hubConnection;
                _hubConnection._logScope.ConnectionId = connection.ConnectionId;
                Connection = connection;
            }

            public string GetNextId() => Interlocked.Increment(ref _nextInvocationId).ToString(CultureInfo.InvariantCulture);
            public string GetNextStreamId() => Interlocked.Increment(ref _nextStreamId).ToString(CultureInfo.InvariantCulture);

            public void AddInvocation(InvocationRequest irq)
            {
                lock (_lock)
                {
                    if (_pendingCalls.ContainsKey(irq.InvocationId))
                    {
                        Log.InvocationAlreadyInUse(_hubConnection._logger, irq.InvocationId);
                        throw new InvalidOperationException($"Invocation ID '{irq.InvocationId}' is already in use.");
                    }
                    else
                    {
                        _pendingCalls.Add(irq.InvocationId, irq);
                    }
                }
            }

            public bool TryGetInvocation(string invocationId, out InvocationRequest irq)
            {
                lock (_lock)
                {
                    return _pendingCalls.TryGetValue(invocationId, out irq);
                }
            }

            public bool TryRemoveInvocation(string invocationId, out InvocationRequest irq)
            {
                lock (_lock)
                {
                    if (_pendingCalls.TryGetValue(invocationId, out irq))
                    {
                        _pendingCalls.Remove(invocationId);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            public void CancelOutstandingInvocations(Exception exception)
            {
                Log.CancelingOutstandingInvocations(_hubConnection._logger);

                lock (_lock)
                {
                    foreach (var outstandingCall in _pendingCalls.Values)
                    {
                        Log.RemovingInvocation(_hubConnection._logger, outstandingCall.InvocationId);
                        if (exception != null)
                        {
                            outstandingCall.Fail(exception);
                        }
                        outstandingCall.Dispose();
                    }
                    _pendingCalls.Clear();
                }
            }

            public Task StopAsync()
            {
                // We want multiple StopAsync calls on the same connection state
                // to wait for the same "stop" to complete.
                lock (_lock)
                {
                    if (_stopTcs != null)
                    {
                        return _stopTcs.Task;
                    }
                    else
                    {
                        _stopTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                        return StopAsyncCore();
                    }
                }
            }

            private async Task StopAsyncCore()
            {
                Log.Stopping(_hubConnection._logger);

                // Complete our write pipe, which should cause everything to shut down
                Log.TerminatingReceiveLoop(_hubConnection._logger);
                Connection.Transport.Input.CancelPendingRead();

                // Wait ServerTimeout for the server or transport to shut down.
                Log.WaitingForReceiveLoopToTerminate(_hubConnection._logger);
                await ReceiveTask;

                Log.Stopped(_hubConnection._logger);

                _hubConnection._logScope.ConnectionId = null;
                _stopTcs.TrySetResult(null);
            }

            Type IInvocationBinder.GetReturnType(string invocationId)
            {
                if (!TryGetInvocation(invocationId, out var irq))
                {
                    Log.ReceivedUnexpectedResponse(_hubConnection._logger, invocationId);
                    return null;
                }
                return irq.ResultType;
            }

            Type IInvocationBinder.GetStreamItemType(string invocationId)
            {
                // previously, streaming was only server->client, and used GetReturnType for StreamItems
                // literally the same code as the above method
                if (!TryGetInvocation(invocationId, out var irq))
                {
                    Log.ReceivedUnexpectedResponse(_hubConnection._logger, invocationId);
                    return null;
                }
                return irq.ResultType;
            }

            IReadOnlyList<Type> IInvocationBinder.GetParameterTypes(string methodName)
            {
                if (!_hubConnection._handlers.TryGetValue(methodName, out var invocationHandlerList))
                {
                    Log.MissingHandler(_hubConnection._logger, methodName);
                    return Type.EmptyTypes;
                }

                // We use the parameter types of the first handler
                var handlers = invocationHandlerList.GetHandlers();
                if (handlers.Length > 0)
                {
                    return handlers[0].ParameterTypes;
                }
                throw new InvalidOperationException($"There are no callbacks registered for the method '{methodName}'");
            }
        }
    }
}
