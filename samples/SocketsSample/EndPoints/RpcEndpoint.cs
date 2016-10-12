using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Channels;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SocketsSample.Protobuf;

namespace SocketsSample
{
    // This end point implementation is used for framing JSON objects from the stream
    public class RpcEndpoint : EndPoint
    {
        private readonly Dictionary<string, Func<InvocationDescriptor, InvocationResultDescriptor>> _callbacks
            = new Dictionary<string, Func<InvocationDescriptor, InvocationResultDescriptor>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Type[]> _paramTypes = new Dictionary<string, Type[]>();

        private readonly ILogger<RpcEndpoint> _logger;
        private readonly IServiceProvider _serviceProvider;


        public RpcEndpoint(ILogger<RpcEndpoint> logger, IServiceProvider serviceProvider)
        {
            // TODO: Discover end points
            _logger = logger;
            _serviceProvider = serviceProvider;

            DiscoverEndpoints();
        }

        protected virtual void DiscoverEndpoints()
        {
            RegisterRPCEndPoint(typeof(Echo));
        }

        public override async Task OnConnected(Connection connection)
        {
            // TODO: Dispatch from the caller
            await Task.Yield();

            var stream = connection.Channel.GetStream();
            var invocationAdapter = _serviceProvider.GetRequiredService<InvocationAdapterRegistry>()
                .GetInvocationAdapter((string)connection.Metadata["formatType"]);

            while (true)
            {
                var invocationDescriptor =
                    await invocationAdapter.CreateInvocationDescriptor(
                            stream, methodName => {
                                Type[] types;
                                // TODO: null or throw?
                                return _paramTypes.TryGetValue(methodName, out types) ? types : null;
                            });

                /* TODO: ?? */
                //if (((Channel)connection.Channel).Reading.IsCompleted)
                //{
                //    break;
                //}

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Received RPC request: {request}", invocationDescriptor.ToString());
                }

                InvocationResultDescriptor result;
                Func<InvocationDescriptor, InvocationResultDescriptor> callback;
                if (_callbacks.TryGetValue(invocationDescriptor.Method, out callback))
                {
                    result = callback(invocationDescriptor);
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

                await invocationAdapter.WriteInvocationResult(stream, result);
            }
        }

        protected virtual void Initialize(object endpoint)
        {
        }

        protected void RegisterRPCEndPoint(Type type)
        {
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

                _callbacks[methodName] = invocationDescriptor =>
                {
                    var invocationResult = new InvocationResultDescriptor();
                    invocationResult.Id = invocationDescriptor.Id;

                    var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();

                    // Scope per call so that deps injected get disposed
                    using (var scope = scopeFactory.CreateScope())
                    {
                        object value = scope.ServiceProvider.GetService(type) ?? Activator.CreateInstance(type);

                        Initialize(value);

                        try
                        {
                            var args = invocationDescriptor.Arguments
                                .Zip(parameters, (a, p) => Convert.ChangeType(a, p.ParameterType))
                                .ToArray();

                            invocationResult.Result = methodInfo.Invoke(value, args);
                        }
                        catch (TargetInvocationException ex)
                        {
                            invocationResult.Error = ex.InnerException.Message;
                        }
                        catch (Exception ex)
                        {
                            invocationResult.Error = ex.Message;
                        }
                    }

                    return invocationResult;
                };
            };
        }
    }
}
