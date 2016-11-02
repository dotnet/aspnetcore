using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Channels;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SocketsSample
{
    public class RpcEndpoint<T> : EndPoint where T : class
    {
        private readonly Dictionary<string, Func<Connection, InvocationDescriptor, InvocationResultDescriptor>> _callbacks
            = new Dictionary<string, Func<Connection, InvocationDescriptor, InvocationResultDescriptor>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Type[]> _paramTypes = new Dictionary<string, Type[]>();

        private readonly ILogger _logger;
        private readonly InvocationAdapterRegistry _registry;
        protected readonly IServiceScopeFactory _serviceScopeFactory;

        public RpcEndpoint(InvocationAdapterRegistry registry, ILoggerFactory loggerFactory, IServiceScopeFactory serviceScopeFactory)
        {
            _logger = loggerFactory.CreateLogger<RpcEndpoint<T>>();
            _registry = registry;
            _serviceScopeFactory = serviceScopeFactory;

            RegisterRPCEndPoint();
        }

        public override async Task OnConnected(Connection connection)
        {
            // TODO: Dispatch from the caller
            await Task.Yield();

            var stream = connection.Channel.GetStream();
            var invocationAdapter = _registry.GetInvocationAdapter((string)connection.Metadata["formatType"]);

            while (true)
            {
                var invocationDescriptor =
                    await invocationAdapter.ReadInvocationDescriptor(
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
                    _logger.LogDebug("Received RPC request: {request}", invocationDescriptor.ToString());
                }

                InvocationResultDescriptor result;
                Func<Connection, InvocationDescriptor, InvocationResultDescriptor> callback;
                if (_callbacks.TryGetValue(invocationDescriptor.Method, out callback))
                {
                    result = callback(connection, invocationDescriptor);
                }
                else
                {
                    // If there's no method then return a failed response for this request
                    result = new InvocationResultDescriptor
                    {
                        Id = invocationDescriptor.Id,
                        Error = $"Unknown method '{invocationDescriptor.Method}'"
                    };
                }

                await invocationAdapter.WriteInvocationResult(result, stream);
            }
        }

        protected virtual void BeforeInvoke(Connection connection, T endpoint)
        {
        }

        protected virtual void AfterInvoke(Connection connection, T endpoint)
        {

        }

        protected void RegisterRPCEndPoint()
        {
            var type = typeof(T);

            foreach (var methodInfo in type.GetTypeInfo().DeclaredMethods.Where(m => m.IsPublic))
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
                    _logger.LogDebug("RPC method '{methodName}' is bound", methodName);
                }

                _callbacks[methodName] = (connection, invocationDescriptor) =>
                {
                    var invocationResult = new InvocationResultDescriptor()
                    {
                        Id = invocationDescriptor.Id
                    };

                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var value = scope.ServiceProvider.GetService<T>() ?? Activator.CreateInstance<T>();

                        BeforeInvoke(connection, value);

                        try
                        {
                            var arguments = invocationDescriptor.Arguments ?? Array.Empty<object>();

                            var args = arguments
                                .Zip(parameters, (a, p) => Convert.ChangeType(a, p.ParameterType))
                                .ToArray();

                            invocationResult.Result = methodInfo.Invoke(value, args);
                        }
                        catch (TargetInvocationException ex)
                        {
                            _logger.LogError(0, ex, "Failed to invoke RPC method");
                            invocationResult.Error = ex.InnerException.Message;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(0, ex, "Failed to invoke RPC method");
                            invocationResult.Error = ex.Message;
                        }
                        finally
                        {
                            AfterInvoke(connection, value);
                        }
                    }

                    return invocationResult;
                };
            };
        }
    }
}
