// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Channels;
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
                           IServiceScopeFactory serviceScopeFactory) : base(lifetimeManager, hubContext, registry, logger, serviceScopeFactory)
        {

        }
    }

    public class HubEndPoint<THub, TClient> : EndPoint where THub : Hub<TClient>
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

                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var hub = scope.ServiceProvider.GetService<THub>() ?? Activator.CreateInstance<THub>();
                    InitializeHub(connection, hub);
                    await hub.OnConnectedAsync();
                }

                await DispatchMessagesAsync(connection);
            }
            finally
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var hub = scope.ServiceProvider.GetService<THub>() ?? Activator.CreateInstance<THub>();
                    InitializeHub(connection, hub);
                    await hub.OnDisconnectedAsync();
                }

                await _lifetimeManager.OnDisconnectedAsync(connection);
            }
        }

        private async Task DispatchMessagesAsync(Connection connection)
        {
            var stream = connection.Channel.GetStream();
            var invocationAdapter = _registry.GetInvocationAdapter(connection.Metadata.Get<string>("formatType"));

            while (true)
            {
                var invocationDescriptor =
                    await invocationAdapter.ReadInvocationDescriptorAsync(
                            stream, methodName =>
                            {
                                Type[] types;
                                // TODO: null or throw?
                                return _paramTypes.TryGetValue(methodName, out types) ? types : null;
                            });

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

                await invocationAdapter.WriteInvocationResultAsync(result, stream);
            }
        }

        private void InitializeHub(Connection connection, THub hub)
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
                var methodName = type.FullName + "." + methodInfo.Name;

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
                        var hub = scope.ServiceProvider.GetService<THub>();

                        bool created = false;

                        if (hub == null)
                        {
                            hub = Activator.CreateInstance<THub>();
                            created = true;
                        }

                        InitializeHub(connection, hub);

                        try
                        {
                            var arguments = invocationDescriptor.Arguments ?? Array.Empty<object>();

                            var args = arguments
                                .Zip(parameters, (a, p) => Convert.ChangeType(a, p.ParameterType))
                                .ToArray();

                            var result = methodInfo.Invoke(hub, args);
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

                        if (created)
                        {
                            // Dispose the object if it's disposable and we created it
                            (hub as IDisposable)?.Dispose();
                        }
                    }

                    return invocationResult;
                };
            };
        }

        private static bool IsHubMethod(MethodInfo m)
        {
            // TODO: Add more checks
            return m.IsPublic;
        }
    }
}
