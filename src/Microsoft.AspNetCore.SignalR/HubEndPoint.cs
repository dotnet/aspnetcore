// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubEndPoint<THub> : HubEndPoint<THub, IClientProxy> where THub : Hub<IClientProxy>
    {
        public HubEndPoint(HubLifetimeManager<THub> lifetimeManager,
                           IHubContext<THub> hubContext,
                           InvocationAdapterRegistry registry,
                           ILogger<HubEndPoint<THub>> logger,
                           IServiceScopeFactory serviceScopeFactory)
            : base(lifetimeManager, hubContext, registry, logger, serviceScopeFactory)
        {
        }
    }

    public class HubEndPoint<THub, TClient> : EndPoint, IInvocationBinder where THub : Hub<TClient>
    {
        private readonly Dictionary<string, Func<Connection, InvocationDescriptor, Task<InvocationResultDescriptor>>> _callbacks
            = new Dictionary<string, Func<Connection, InvocationDescriptor, Task<InvocationResultDescriptor>>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Type[]> _paramTypes = new Dictionary<string, Type[]>();

        private readonly HubLifetimeManager<THub> _lifetimeManager;
        private readonly IHubContext<THub, TClient> _hubContext;
        private readonly ILogger<HubEndPoint<THub, TClient>> _logger;
        private readonly InvocationAdapterRegistry _registry;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public HubEndPoint(HubLifetimeManager<THub> lifetimeManager,
                           IHubContext<THub, TClient> hubContext,
                           InvocationAdapterRegistry registry,
                           ILogger<HubEndPoint<THub, TClient>> logger,
                           IServiceScopeFactory serviceScopeFactory)
        {
            _lifetimeManager = lifetimeManager;
            _hubContext = hubContext;
            _registry = registry;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;

            DiscoverHubMethods();
        }

        public override async Task OnConnectedAsync(Connection connection)
        {
            // TODO: Dispatch from the caller
            await Task.Yield();

            try
            {
                await _lifetimeManager.OnConnectedAsync(connection);
                await RunHubAsync(connection);
            }
            finally
            {
                await _lifetimeManager.OnDisconnectedAsync(connection);
            }
        }

        private async Task RunHubAsync(Connection connection)
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

        private async Task HubOnConnectedAsync(Connection connection)
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

        private async Task HubOnDisconnectedAsync(Connection connection, Exception exception)
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

        private async Task DispatchMessagesAsync(Connection connection)
        {
            var invocationAdapter = _registry.GetInvocationAdapter(connection.Metadata.Get<string>("formatType"));

            while (await connection.Transport.Input.WaitToReadAsync())
            {
                Message incomingMessage;
                while (connection.Transport.Input.TryRead(out incomingMessage))
                {
                    InvocationDescriptor invocationDescriptor;
                    using (incomingMessage)
                    {
                        var inputStream = new MemoryStream(incomingMessage.Payload.Buffer.ToArray());

                        // TODO: Handle receiving InvocationResultDescriptor
                        invocationDescriptor = await invocationAdapter.ReadMessageAsync(inputStream, this) as InvocationDescriptor;
                    }

                    // Is there a better way of detecting that a connection was closed?
                    if (invocationDescriptor == null)
                    {
                        break;
                    }

                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("Received hub invocation: {invocation}", invocationDescriptor);
                    }

                    InvocationResultDescriptor result;
                    Func<Connection, InvocationDescriptor, Task<InvocationResultDescriptor>> callback;
                    if (_callbacks.TryGetValue(invocationDescriptor.Method, out callback))
                    {
                        result = await callback(connection, invocationDescriptor);
                    }
                    else
                    {
                        // If there's no method then return a failed response for this request
                        result = new InvocationResultDescriptor
                        {
                            Id = invocationDescriptor.Id,
                            Error = $"Unknown hub method '{invocationDescriptor.Method}'"
                        };

                        _logger.LogError("Unknown hub method '{method}'", invocationDescriptor.Method);
                    }

                    // TODO: Pool memory
                    var outStream = new MemoryStream();
                    await invocationAdapter.WriteMessageAsync(result, outStream);

                    var buffer = ReadableBuffer.Create(outStream.ToArray()).Preserve();
                    var outMessage = new Message(buffer, Format.Text, endOfMessage: true);

                    while (await connection.Transport.Output.WaitToWriteAsync())
                    {
                        if (connection.Transport.Output.TryWrite(outMessage))
                        {
                            break;
                        }
                    }
                }
            }
        }

        private void InitializeHub(THub hub, Connection connection)
        {
            hub.Clients = _hubContext.Clients;
            hub.Context = new HubCallerContext(connection);
            hub.Groups = new GroupManager<THub>(connection, _lifetimeManager);
        }

        private void DiscoverHubMethods()
        {
            var type = typeof(THub);

            foreach (var methodInfo in type.GetTypeInfo().DeclaredMethods.Where(m => IsHubMethod(m)))
            {
                var methodName = methodInfo.Name;

                if (_callbacks.ContainsKey(methodName))
                {
                    throw new NotSupportedException($"Duplicate definitions of '{methodInfo.Name}'. Overloading is not supported.");
                }

                var parameters = methodInfo.GetParameters();
                _paramTypes[methodName] = parameters.Select(p => p.ParameterType).ToArray();

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Hub method '{methodName}' is bound", methodName);
                }

                _callbacks[methodName] = async (connection, invocationDescriptor) =>
                {
                    var invocationResult = new InvocationResultDescriptor()
                    {
                        Id = invocationDescriptor.Id
                    };

                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var hubActivator = scope.ServiceProvider.GetRequiredService<IHubActivator<THub, TClient>>();
                        var hub = hubActivator.Create();

                        try
                        {
                            InitializeHub(hub, connection);

                            var result = methodInfo.Invoke(hub, invocationDescriptor.Arguments);
                            var resultTask = result as Task;
                            if (resultTask != null)
                            {
                                await resultTask;
                                if (methodInfo.ReturnType.GetTypeInfo().IsGenericType)
                                {
                                    var property = resultTask.GetType().GetProperty("Result");
                                    invocationResult.Result = property?.GetValue(resultTask);
                                }
                            }
                            else
                            {
                                invocationResult.Result = result;
                            }
                        }
                        catch (TargetInvocationException ex)
                        {
                            _logger.LogError(0, ex, "Failed to invoke hub method");
                            invocationResult.Error = ex.InnerException.Message;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(0, ex, "Failed to invoke hub method");
                            invocationResult.Error = ex.Message;
                        }
                        finally
                        {
                            hubActivator.Release(hub);
                        }
                    }

                    return invocationResult;
                };
            };
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
            Type[] types;
            if (!_paramTypes.TryGetValue(methodName, out types))
            {
                return Type.EmptyTypes;
            }
            return types;
        }
    }
}
