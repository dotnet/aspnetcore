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

namespace SocketsSample
{
    // This end point implementation is used for framing JSON objects from the stream
    public class RpcEndpoint : EndPoint
    {
        private readonly Dictionary<string, Func<InvocationDescriptor, InvocationResultDescriptor>> _callbacks
            = new Dictionary<string, Func<InvocationDescriptor, InvocationResultDescriptor>>(StringComparer.OrdinalIgnoreCase);

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

            var formatterFactory = _serviceProvider.GetRequiredService<IFormatterFactory>();
            var formatType = (string)connection.Metadata["formatType"];
            var formatter = formatterFactory.CreateFormatter(connection.Metadata.Format, formatType);

            while (true)
            {
                // JSON.NET doesn't handle async reads so we wait for data here
                var result = await connection.Channel.Input.ReadAsync();

                // Don't advance the buffer so we parse sync
                connection.Channel.Input.Advance(result.Buffer.Start);

                while (!reader.Read())
                {
                    break;
                }

                JObject request;
                try
                {
                    request = JObject.Load(reader);
                }
                catch (Exception)
                {
                    if (result.IsCompleted)
                    {
                        break;
                    }

                    throw;
                }

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Received JSON RPC request: {request}", invocationDescriptor.ToString());
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

                await formatter.WriteAsync(result, connection.Channel.GetStream());
            }
        }

        protected virtual void Initialize(object endpoint)
        {
        }

        protected void RegisterRPCEndPoint(Type type)
        {
            var methods = new List<string>();

            foreach (var m in type.GetTypeInfo().DeclaredMethods.Where(m => m.IsPublic))
            {
                var methodName = type.FullName + "." + m.Name;

                methods.Add(methodName);

                var parameters = m.GetParameters();

                if (_callbacks.ContainsKey(methodName))
                {
                    throw new NotSupportedException(String.Format("Duplicate definitions of {0}. Overloading is not supported.", m.Name));
                }

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

                            invocationResult.Result = m.Invoke(value, args);
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
