// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubEndPoint<THub> : HubEndPoint<THub, IClientProxy> where THub : Hub<IClientProxy>
    {
        public HubEndPoint(HubLifetimeManager<THub> lifetimeManager,
                           IHubProtocolResolver protocolResolver,
                           IHubContext<THub> hubContext,
                           ILogger<HubEndPoint<THub>> logger,
                           IServiceScopeFactory serviceScopeFactory)
            : base(lifetimeManager, protocolResolver, hubContext, logger, serviceScopeFactory)
        {
        }
    }

    public class HubEndPoint<THub, TClient> : IInvocationBinder where THub : Hub<TClient>
    {
        private readonly Dictionary<string, HubMethodDescriptor> _methods = new Dictionary<string, HubMethodDescriptor>(StringComparer.OrdinalIgnoreCase);

        private readonly HubLifetimeManager<THub> _lifetimeManager;
        private readonly IHubContext<THub, TClient> _hubContext;
        private readonly ILogger<HubEndPoint<THub, TClient>> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IHubProtocolResolver _protocolResolver;

        public HubEndPoint(HubLifetimeManager<THub> lifetimeManager,
                           IHubProtocolResolver protocolResolver,
                           IHubContext<THub, TClient> hubContext,
                           ILogger<HubEndPoint<THub, TClient>> logger,
                           IServiceScopeFactory serviceScopeFactory)
        {
            _protocolResolver = protocolResolver;
            _lifetimeManager = lifetimeManager;
            _hubContext = hubContext;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;

            DiscoverHubMethods();
        }

        public async Task OnConnectedAsync(ConnectionContext connection)
        {
            try
            {
                // Resolve the Hub Protocol for the connection and store it in metadata
                // Other components, outside the Hub, may need to know what protocol is in use
                // for a particular connection, so we store it here.
                connection.Metadata[HubConnectionMetadataNames.HubProtocol] = _protocolResolver.GetProtocol(connection);

                await _lifetimeManager.OnConnectedAsync(connection);
                await RunHubAsync(connection);
            }
            finally
            {
                await _lifetimeManager.OnDisconnectedAsync(connection);
            }
        }

        private async Task RunHubAsync(ConnectionContext connection)
        {
            await HubOnConnectedAsync(connection);

            try
            {
                await DispatchMessagesAsync(connection);
            }
            catch (Exception ex)
            {
                _logger.LogError(0, ex, "Error when processing requests.");
                await HubOnDisconnectedAsync(connection, ex);
                throw;
            }

            await HubOnDisconnectedAsync(connection, null);
        }

        private async Task HubOnConnectedAsync(ConnectionContext connection)
        {
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var hubActivator = scope.ServiceProvider.GetRequiredService<IHubActivator<THub, TClient>>();
                    var hub = hubActivator.Create();
                    try
                    {
                        InitializeHub(hub, connection);
                        await hub.OnConnectedAsync();
                    }
                    finally
                    {
                        hubActivator.Release(hub);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(0, ex, "Error when invoking OnConnectedAsync on hub.");
                throw;
            }
        }

        private async Task HubOnDisconnectedAsync(ConnectionContext connection, Exception exception)
        {
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var hubActivator = scope.ServiceProvider.GetRequiredService<IHubActivator<THub, TClient>>();
                    var hub = hubActivator.Create();
                    try
                    {
                        InitializeHub(hub, connection);
                        await hub.OnDisconnectedAsync(exception);
                    }
                    finally
                    {
                        hubActivator.Release(hub);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(0, ex, "Error when invoking OnDisconnectedAsync on hub.");
                throw;
            }
        }

        private async Task DispatchMessagesAsync(ConnectionContext connection)
        {
            // We use these for error handling. Since we dispatch multiple hub invocations
            // in parallel, we need a way to communicate failure back to the main processing loop. The
            // cancellation token is used to stop reading from the channel, the tcs
            // is used to get the exception so we can bubble it up the stack
            var cts = new CancellationTokenSource();
            var completion = new TaskCompletionSource<object>();
            var protocol = connection.Metadata.Get<IHubProtocol>(HubConnectionMetadataNames.HubProtocol);

            try
            {
                while (await connection.Transport.Input.WaitToReadAsync(cts.Token))
                {
                    while (connection.Transport.Input.TryRead(out var incomingMessage))
                    {
                        var hubMessage = protocol.ParseMessage(incomingMessage.Payload, this);

                        switch (hubMessage)
                        {
                            case InvocationMessage invocationMessage:
                                if (_logger.IsEnabled(LogLevel.Debug))
                                {
                                    _logger.LogDebug("Received hub invocation: {invocation}", invocationMessage);
                                }

                                // Don't wait on the result of execution, continue processing other
                                // incoming messages on this connection.
                                var ignore = ProcessInvocation(connection, protocol, invocationMessage, cts, completion);
                                break;

                            // Other kind of message we weren't expecting
                            default:
                                _logger.LogError("Received unsupported message of type '{messageType}'", hubMessage.GetType().FullName);
                                throw new NotSupportedException($"Received unsupported message: {hubMessage}");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Await the task so the exception bubbles up to the caller
                await completion.Task;
            }
        }

        private async Task ProcessInvocation(ConnectionContext connection,
                                             IHubProtocol protocol,
                                             InvocationMessage invocationMessage,
                                             CancellationTokenSource dispatcherCancellation,
                                             TaskCompletionSource<object> dispatcherCompletion)
        {
            try
            {
                // If an unexpected exception occurs then we want to kill the entire connection
                // by ending the processing loop
                await Execute(connection, protocol, invocationMessage);
            }
            catch (Exception ex)
            {
                // Set the exception on the task completion source
                dispatcherCompletion.TrySetException(ex);

                // Cancel reading operation
                dispatcherCancellation.Cancel();
            }
        }

        private async Task Execute(ConnectionContext connection, IHubProtocol protocol, InvocationMessage invocationMessage)
        {
            HubMethodDescriptor descriptor;
            if (!_methods.TryGetValue(invocationMessage.Target, out descriptor))
            {
                // Send an error to the client. Then let the normal completion process occur
                _logger.LogError("Unknown hub method '{method}'", invocationMessage.Target);
                await SendMessageAsync(connection, protocol, CompletionMessage.WithError(invocationMessage.InvocationId, $"Unknown hub method '{invocationMessage.Target}'"));
            }
            else
            {
                await Invoke(descriptor, connection, protocol, invocationMessage);
            }
        }

        private async Task SendMessageAsync(ConnectionContext connection, IHubProtocol protocol, HubMessage hubMessage)
        {
            var payload = await protocol.WriteToArrayAsync(hubMessage);
            var message = new Message(payload, protocol.MessageType, endOfMessage: true);

            while (await connection.Transport.Output.WaitToWriteAsync())
            {
                if (connection.Transport.Output.TryWrite(message))
                {
                    return;
                }
            }

            // Output is closed. Cancel this invocation completely
            _logger.LogWarning("Outbound channel was closed while trying to write hub message");
            throw new OperationCanceledException("Outbound channel was closed while trying to write hub message");
        }

        private async Task Invoke(HubMethodDescriptor descriptor, ConnectionContext connection, IHubProtocol protocol, InvocationMessage invocationMessage)
        {
            var methodExecutor = descriptor.MethodExecutor;

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var hubActivator = scope.ServiceProvider.GetRequiredService<IHubActivator<THub, TClient>>();
                var hub = hubActivator.Create();

                try
                {
                    InitializeHub(hub, connection);

                    object result = null;

                    // ReadableChannel is awaitable but we don't want to await it.
                    if (methodExecutor.IsMethodAsync && !IsChannel(methodExecutor.MethodReturnType, out _))
                    {
                        if (methodExecutor.MethodReturnType == typeof(Task))
                        {
                            await (Task)methodExecutor.Execute(hub, invocationMessage.Arguments);
                        }
                        else
                        {
                            result = await methodExecutor.ExecuteAsync(hub, invocationMessage.Arguments);
                        }
                    }
                    else
                    {
                        result = methodExecutor.Execute(hub, invocationMessage.Arguments);
                    }

                    if (IsStreamed(methodExecutor, result, methodExecutor.MethodReturnType, out var enumerator))
                    {
                        _logger.LogTrace("[{connectionId}/{invocationId}] Streaming result of type {resultType}", connection.ConnectionId, invocationMessage.InvocationId, methodExecutor.MethodReturnType.FullName);
                        await StreamResultsAsync(invocationMessage.InvocationId, connection, protocol, enumerator);
                    }
                    else
                    {
                        _logger.LogTrace("[{connectionId}/{invocationId}] Sending result of type {resultType}", connection.ConnectionId, invocationMessage.InvocationId, methodExecutor.MethodReturnType.FullName);
                        await SendMessageAsync(connection, protocol, CompletionMessage.WithResult(invocationMessage.InvocationId, result));
                    }
                }
                catch (TargetInvocationException ex)
                {
                    _logger.LogError(0, ex, "Failed to invoke hub method");
                    await SendMessageAsync(connection, protocol, CompletionMessage.WithError(invocationMessage.InvocationId, ex.InnerException.Message));
                }
                catch (Exception ex)
                {
                    _logger.LogError(0, ex, "Failed to invoke hub method");
                    await SendMessageAsync(connection, protocol, CompletionMessage.WithError(invocationMessage.InvocationId, ex.Message));
                }
                finally
                {
                    hubActivator.Release(hub);
                }
            }
        }

        private void InitializeHub(THub hub, ConnectionContext connection)
        {
            hub.Clients = _hubContext.Clients;
            hub.Context = new HubCallerContext(connection);
            hub.Groups = new GroupManager<THub>(connection, _lifetimeManager);
        }

        private bool IsChannel(Type type, out Type payloadType)
        {
            var channelType = type.AllBaseTypes().FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ReadableChannel<>));
            if (channelType == null)
            {
                payloadType = null;
                return false;
            }
            else
            {
                payloadType = channelType.GetGenericArguments()[0];
                return true;
            }
        }

        private async Task StreamResultsAsync(string invocationId, ConnectionContext connection, IHubProtocol protocol, IAsyncEnumerator<object> enumerator)
        {
            // TODO: Cancellation? See https://github.com/aspnet/SignalR/issues/481
            try
            {
                while (await enumerator.MoveNextAsync())
                {
                    // Send the stream item
                    await SendMessageAsync(connection, protocol, new StreamItemMessage(invocationId, enumerator.Current));
                }

                await SendMessageAsync(connection, protocol, CompletionMessage.Empty(invocationId));
            }
            catch (Exception ex)
            {
                await SendMessageAsync(connection, protocol, CompletionMessage.WithError(invocationId, ex.Message));
            }
        }

        private bool IsStreamed(ObjectMethodExecutor methodExecutor, object result, Type resultType, out IAsyncEnumerator<object> enumerator)
        {
            if (result == null)
            {
                enumerator = null;
                return false;
            }

            var observableInterface = IsIObservable(resultType) ?
                resultType :
                resultType.GetInterfaces().FirstOrDefault(IsIObservable);
            if (observableInterface != null)
            {
                enumerator = AsyncEnumeratorAdapters.FromObservable(result, observableInterface);
                return true;
            }
            else if (IsChannel(resultType, out var payloadType))
            {
                enumerator = AsyncEnumeratorAdapters.FromChannel(result, payloadType);
                return true;
            }
            else
            {
                // Not streamed
                enumerator = null;
                return false;
            }
        }

        private static bool IsIObservable(Type iface)
        {
            return iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IObservable<>);
        }

        private void DiscoverHubMethods()
        {
            var hubType = typeof(THub);
            foreach (var methodInfo in hubType.GetMethods().Where(m => IsHubMethod(m)))
            {
                var methodName = methodInfo.Name;

                if (_methods.ContainsKey(methodName))
                {
                    throw new NotSupportedException($"Duplicate definitions of '{methodName}'. Overloading is not supported.");
                }

                var executor = ObjectMethodExecutor.Create(methodInfo, hubType.GetTypeInfo());
                _methods[methodName] = new HubMethodDescriptor(executor);

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Hub method '{methodName}' is bound", methodName);
                }
            }
        }

        private static bool IsHubMethod(MethodInfo methodInfo)
        {
            // TODO: Add more checks
            if (!methodInfo.IsPublic || methodInfo.IsSpecialName)
            {
                return false;
            }

            var baseDefinition = methodInfo.GetBaseDefinition().DeclaringType;
            var baseType = baseDefinition.GetTypeInfo().IsGenericType ? baseDefinition.GetGenericTypeDefinition() : baseDefinition;
            if (typeof(Hub<>) == baseType)
            {
                return false;
            }

            return true;
        }

        Type IInvocationBinder.GetReturnType(string invocationId)
        {
            return typeof(object);
        }

        Type[] IInvocationBinder.GetParameterTypes(string methodName)
        {
            HubMethodDescriptor descriptor;
            if (!_methods.TryGetValue(methodName, out descriptor))
            {
                return Type.EmptyTypes;
            }
            return descriptor.ParameterTypes;
        }

        // REVIEW: We can decide to move this out of here if we want pluggable hub discovery
        private class HubMethodDescriptor
        {
            public HubMethodDescriptor(ObjectMethodExecutor methodExecutor)
            {
                MethodExecutor = methodExecutor;
                ParameterTypes = methodExecutor.MethodParameters.Select(p => p.ParameterType).ToArray();
            }

            public ObjectMethodExecutor MethodExecutor { get; }

            public Type[] ParameterTypes { get; }
        }
    }
}
