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
    public class JsonRpcEndpoint : EndPoint
    {
        private readonly Dictionary<string, Func<JObject, JObject>> _callbacks = new Dictionary<string, Func<JObject, JObject>>(StringComparer.OrdinalIgnoreCase);
        private readonly ILogger<JsonRpcEndpoint> _logger;
        private readonly IServiceProvider _serviceProvider;

        public JsonRpcEndpoint(ILogger<JsonRpcEndpoint> logger, IServiceProvider serviceProvider)
        {
            // TODO: Discover end points
            _logger = logger;
            _serviceProvider = serviceProvider;

            DiscoverEndpoints();
        }

        protected virtual void DiscoverEndpoints()
        {
            RegisterJsonRPCEndPoint(typeof(Echo));
        }

        public override async Task OnConnected(Connection connection)
        {
            // TODO: Dispatch from the caller
            await Task.Yield();

            // DO real async reads
            var stream = connection.Channel.GetStream();
            var reader = new JsonTextReader(new StreamReader(stream));
            reader.SupportMultipleContent = true;

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
                    _logger.LogDebug("Received JSON RPC request: {request}", request);
                }

                JObject response = null;

                Func<JObject, JObject> callback;
                if (_callbacks.TryGetValue(request.Value<string>("method"), out callback))
                {
                    response = callback(request);
                }
                else
                {
                    // If there's no method then return a failed response for this request
                    response = new JObject();
                    response["id"] = request["id"];
                    response["error"] = string.Format("Unknown method '{0}'", request.Value<string>("method"));
                }

                _logger.LogDebug("Sending JSON RPC response: {data}", response);

                var writer = new JsonTextWriter(new StreamWriter(stream));
                response.WriteTo(writer);
                writer.Flush();
            }
        }

        protected virtual void Initialize(object endpoint)
        {

        }

        protected void RegisterJsonRPCEndPoint(Type type)
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

                _callbacks[methodName] = request =>
                {
                    var response = new JObject();
                    response["id"] = request["id"];

                    var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();

                    // Scope per call so that deps injected get disposed
                    using (var scope = scopeFactory.CreateScope())
                    {
                        object value = scope.ServiceProvider.GetService(type) ?? Activator.CreateInstance(type);

                        Initialize(value);

                        try
                        {
                            var args = request.Value<JArray>("params").Zip(parameters, (a, p) => a.ToObject(p.ParameterType))
                                                                      .ToArray();

                            var result = m.Invoke(value, args);

                            if (result != null)
                            {
                                response["result"] = JToken.FromObject(result);
                            }
                        }
                        catch (TargetInvocationException ex)
                        {
                            response["error"] = ex.InnerException.Message;
                        }
                        catch (Exception ex)
                        {
                            response["error"] = ex.Message;
                        }
                    }

                    return response;
                };
            };
        }
    }
}
