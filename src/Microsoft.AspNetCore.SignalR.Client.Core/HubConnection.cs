// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Client.Internal;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public partial class HubConnection
    {
        public static readonly TimeSpan DefaultServerTimeout = TimeSpan.FromSeconds(30); // Server ping rate is 15 sec, this is 2 times that.
        public static readonly TimeSpan DefaultHandshakeTimeout = TimeSpan.FromSeconds(15);

        // This lock protects the connection state.
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);

        // Persistent across all connections
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly IHubProtocol _protocol;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConnectionFactory _connectionFactory;
        private readonly ConcurrentDictionary<string, InvocationHandlerList> _handlers = new ConcurrentDictionary<string, InvocationHandlerList>(StringComparer.Ordinal);
        private bool _disposed;

        // Transient state to a connection
        private ConnectionState _connectionState;

        public event Func<Exception, Task> Closed;

        /// <summary>
        /// Gets or sets the server timeout interval for the connection. Changes to this value
        /// will not be applied until the Keep Alive timer is next reset.
        /// </summary>
        public TimeSpan ServerTimeout { get; set; } = DefaultServerTimeout;
        public TimeSpan HandshakeTimeout { get; set; } = DefaultHandshakeTimeout;

        public HubConnection(IConnectionFactory connectionFactory, IHubProtocol protocol, IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
            : this(connectionFactory, protocol, loggerFactory)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public HubConnection(IConnectionFactory connectionFactory, IHubProtocol protocol, ILoggerFactory loggerFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));

            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            _logger = _loggerFactory.CreateLogger<HubConnection>();
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            CheckDisposed();
            await StartAsyncCore(cancellationToken).ForceAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            CheckDisposed();
            await StopAsyncCore(disposing: false).ForceAsync();
        }

        public async Task DisposeAsync()
        {
            if (!_disposed)
            {
                await StopAsyncCore(disposing: true).ForceAsync();
            }
        }

        public IDisposable On(string methodName, Type[] parameterTypes, Func<object[], object, Task> handler, object state)
        {
            Log.RegisteringHandler(_logger, methodName);

            CheckDisposed();

            // It's OK to be disposed while registering a callback, we'll just never call the callback anyway (as with all the callbacks registered before disposal).
            var invocationHandler = new InvocationHandler(parameterTypes, handler, state);
            var invocationList = _handlers.AddOrUpdate(methodName, _ => new InvocationHandlerList(invocationHandler) ,
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

        public async Task<ChannelReader<object>> StreamAsChannelCoreAsync(string methodName, Type returnType, object[] args, CancellationToken cancellationToken = default) =>
            await StreamAsChannelCoreAsyncCore(methodName, returnType, args, cancellationToken).ForceAsync();

        public async Task<object> InvokeCoreAsync(string methodName, Type returnType, object[] args, CancellationToken cancellationToken = default) =>
            await InvokeCoreAsyncCore(methodName, returnType, args, cancellationToken).ForceAsync();

        // REVIEW: We don't generally use cancellation tokens when writing to a pipe because the asynchrony is only the result of backpressure.
        // However, this would be the only "invocation" method _without_ a cancellation token... which is odd.
        public async Task SendCoreAsync(string methodName, object[] args, CancellationToken cancellationToken = default) =>
            await SendCoreAsyncCore(methodName, args, cancellationToken).ForceAsync();

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
                await connectionState.StopAsync(ServerTimeout);
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

            CheckDisposed();
            await WaitConnectionLockAsync();

            ChannelReader<object> channel;
            try
            {
                CheckDisposed();
                CheckConnectionActive(nameof(StreamAsChannelCoreAsync));

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

            return channel;
        }


        private async Task<object> InvokeCoreAsyncCore(string methodName, Type returnType, object[] args, CancellationToken cancellationToken)
        {
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

            // Wait for this outside the lock, because it won't complete until the server responds.
            return await invocationTask;
        }

        private async Task InvokeCore(string methodName, InvocationRequest irq, object[] args, CancellationToken cancellationToken)
        {
            AssertConnectionValid();

            Log.PreparingBlockingInvocation(_logger, irq.InvocationId, methodName, irq.ResultType.FullName, args.Length);

            // Client invocations are always blocking
            var invocationMessage = new InvocationMessage(irq.InvocationId, methodName, null, args);

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

            var invocationMessage = new StreamInvocationMessage(irq.InvocationId, methodName, null, args);

            // I just want an excuse to use 'irq' as a variable name...
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

        private async Task SendHubMessage(HubInvocationMessage hubMessage, CancellationToken cancellationToken = default)
        {
            AssertConnectionValid();

            _protocol.WriteMessage(hubMessage, _connectionState.Connection.Transport.Output);

            Log.SendingMessage(_logger, hubMessage);

            // REVIEW: If a token is passed in and is canceled during FlushAsync it seems to break .Complete()...
            await _connectionState.Connection.Transport.Output.FlushAsync();

            Log.MessageSent(_logger, hubMessage);
        }

        private async Task SendCoreAsyncCore(string methodName, object[] args, CancellationToken cancellationToken)
        {
            CheckDisposed();

            await WaitConnectionLockAsync();
            try
            {
                CheckDisposed();
                CheckConnectionActive(nameof(SendCoreAsync));

                Log.PreparingNonBlockingInvocation(_logger, methodName, args.Length);

                var invocationMessage = new InvocationMessage(null, methodName, null, args);

                await SendHubMessage(invocationMessage, cancellationToken);
            }
            finally
            {
                ReleaseConnectionLock();
            }
        }

        private async Task<(bool close, Exception exception)> ProcessMessagesAsync(HubMessage message, ConnectionState connectionState)
        {
            InvocationRequest irq;
            switch (message)
            {
                case InvocationMessage invocation:
                    Log.ReceivedInvocation(_logger, invocation.InvocationId, invocation.Target,
                        invocation.ArgumentBindingException != null ? null : invocation.Arguments);
                    await DispatchInvocationAsync(invocation);
                    break;
                case CompletionMessage completion:
                    if (!connectionState.TryRemoveInvocation(completion.InvocationId, out irq))
                    {
                        Log.DroppedCompletionMessage(_logger, completion.InvocationId);
                    }
                    else
                    {
                        DispatchInvocationCompletion(completion, irq);
                        irq.Dispose();
                    }
                    break;
                case StreamItemMessage streamItem:
                    // Complete the invocation with an error, we don't support streaming (yet)
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
                    // Nothing to do on receipt of a ping.
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected message type: {message.GetType().FullName}");
            }

            return (close: false, exception: null);
        }

        private async Task DispatchInvocationAsync(InvocationMessage invocation)
        {
            // Make sure we get off the main event loop before we dispatch into user code
            await AwaitableThreadPool.Yield();

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
                                    // as the the negotiate response.
                                    consumed = buffer.Start;
                                    examined = consumed;

                                    if (message.Error != null)
                                    {
                                        Log.HandshakeServerError(_logger, message.Error);
                                        throw new HubException(
                                            $"Unable to complete handshake with the server due to an error: {message.Error}");
                                    }

                                    break;
                                }
                            }
                            else if (result.IsCompleted)
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
            // Ignore HubException because we throw it when we receive a handshake response with an error
            // And we don't need to log that the handshake failed
            catch (Exception ex) when (!(ex is HubException))
            {
                // shutdown if we're unable to read handshake
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

            var timeoutTimer = StartTimeoutTimer(connectionState);

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
                            ResetTimeoutTimer(timeoutTimer);

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
                        else if (result.IsCompleted)
                        {
                            break;
                        }
                    }
                    finally
                    {
                        // The buffer was sliced up to where it was consumed, so we can just advance to the start.
                        // We mark examined as buffer.End so that if we didn't receive a full frame, we'll wait for more data
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

            // Stop the timeout timer.
            timeoutTimer?.Dispose();

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

        private void ResetTimeoutTimer(Timer timeoutTimer)
        {
            if (timeoutTimer != null)
            {
                Log.ResettingKeepAliveTimer(_logger);
                timeoutTimer.Change(ServerTimeout, Timeout.InfiniteTimeSpan);
            }
        }

        private Timer StartTimeoutTimer(ConnectionState connectionState)
        {
            // Check if we need keep-alive
            Timer timeoutTimer = null;

            // We use '!== true' because it could be null, which we treat as false.
            if (connectionState.Connection.Features.Get<IConnectionInherentKeepAliveFeature>()?.HasInherentKeepAlive != true)
            {
                Log.StartingServerTimeoutTimer(_logger, ServerTimeout);
                timeoutTimer = new Timer(
                    state => OnTimeout((ConnectionState)state),
                    connectionState,
                    dueTime: ServerTimeout,
                    period: Timeout.InfiniteTimeSpan);
            }
            else
            {
                Log.NotUsingServerTimeout(_logger);
            }

            return timeoutTimer;
        }

        private void OnTimeout(ConnectionState connectionState)
        {
            if (!Debugger.IsAttached)
            {
                connectionState.CloseException = new TimeoutException(
                    $"Server timeout ({ServerTimeout.TotalMilliseconds:0.00}ms) elapsed without receiving a message from the server.");
                connectionState.Connection.Transport.Input.CancelPendingRead();
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
            private int _nextId;

            public ConnectionContext Connection { get; }
            public Task ReceiveTask { get; set; }
            public Exception CloseException { get; set; }

            public bool Stopping
            {
                get => _stopping;
                set => _stopping = value;
            }

            public ConnectionState(ConnectionContext connection, HubConnection hubConnection)
            {
                _hubConnection = hubConnection;
                Connection = connection;
            }

            public string GetNextId() => Interlocked.Increment(ref _nextId).ToString(CultureInfo.InvariantCulture);

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

            public Task StopAsync(TimeSpan timeout)
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
                        return StopAsyncCore(timeout);
                    }
                }
            }

            private async Task StopAsyncCore(TimeSpan timeout)
            {
                Log.Stopping(_hubConnection._logger);

                // Complete our write pipe, which should cause everything to shut down
                Log.TerminatingReceiveLoop(_hubConnection._logger);
                Connection.Transport.Input.CancelPendingRead();

                // Wait ServerTimeout for the server or transport to shut down.
                Log.WaitingForReceiveLoopToTerminate(_hubConnection._logger);
                await ReceiveTask;

                Log.Stopped(_hubConnection._logger);
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
