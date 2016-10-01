using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Sockets.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Sockets
{
    public class HttpConnectionDispatcher
    {
        private readonly ConnectionManager _manager = new ConnectionManager();
        private readonly ChannelFactory _channelFactory = new ChannelFactory();
        private readonly RouteBuilder _routes;

        public HttpConnectionDispatcher(IApplicationBuilder app)
        {
            _routes = new RouteBuilder(app);
        }

        public void MapSocketEndpoint<TEndPoint>(string path) where TEndPoint : EndPoint
        {
            _routes.AddPrefixRoute(path, new RouteHandler(c => Execute<TEndPoint>(path, c)));
        }

        public IRouter GetRouter() => _routes.Build();

        public async Task Execute<TEndPoint>(string path, HttpContext context) where TEndPoint : EndPoint
        {
            if (context.Request.Path.StartsWithSegments(path + "/getid"))
            {
                await ProcessGetId(context);
            }
            else if (context.Request.Path.StartsWithSegments(path + "/send"))
            {
                await ProcessSend(context);
            }
            else
            {
                // REVIEW: Errors?

                // Get the end point mapped to this http connection
                var endpoint = (EndPoint)context.RequestServices.GetRequiredService<TEndPoint>();

                // Server sent events transport
                if (context.Request.Path.StartsWithSegments(path + "/sse"))
                {
                    // Get the connection state for the current http context
                    var connectionState = GetOrCreateConnection(context);
                    var sse = new ServerSentEvents(connectionState.Connection);

                    // Register this transport for disconnect
                    RegisterDisconnect(context, sse);

                    // Call into the end point passing the connection
                    var endpointTask = endpoint.OnConnected(connectionState.Connection);

                    // Start the transport
                    await sse.ProcessRequest(context);

                    // Transport has ended so kill the channel
                    connectionState.Connection.Channel.Dispose();

                    // Wait for the user code to unwind
                    await endpointTask;

                    _manager.RemoveConnection(connectionState.Connection.ConnectionId);
                }
                else if (context.Request.Path.StartsWithSegments(path + "/ws"))
                {
                    // Get the connection state for the current http context
                    var connectionState = GetOrCreateConnection(context);
                    var ws = new WebSockets(connectionState.Connection);

                    // Register this transport for disconnect
                    RegisterDisconnect(context, ws);

                    // Call into the end point passing the connection
                    var endpointTask = endpoint.OnConnected(connectionState.Connection);

                    // Start the transport
                    await ws.ProcessRequest(context);

                    // Transport has ended so kill the channel
                    connectionState.Connection.Channel.Dispose();

                    // Wait for the user code to unwind
                    await endpointTask;

                    _manager.RemoveConnection(connectionState.Connection.ConnectionId);
                }
                else if (context.Request.Path.StartsWithSegments(path + "/poll"))
                {
                    var connectionId = context.Request.Query["id"];
                    ConnectionState connectionState;

                    bool isNewConnection = false;
                    if (_manager.TryGetConnection(connectionId, out connectionState))
                    {
                        // Treat reserved connections like new ones
                        if (connectionState.Connection.Channel == null)
                        {
                            // REVIEW: The connection manager should encapsulate this...
                            connectionState.Active = true;
                            connectionState.LastSeen = DateTimeOffset.UtcNow;
                            connectionState.Connection.Channel = new HttpChannel(_channelFactory);
                            isNewConnection = true;
                        }
                    }
                    else
                    {
                        // Add new connection
                        connectionState = _manager.AddNewConnection(new HttpChannel(_channelFactory));
                        isNewConnection = true;
                    }

                    // Raise OnConnected for new connections only since polls happen all the time
                    if (isNewConnection)
                    {
                        // REVIEW: We should await this task after disposing the connection
                        var ignore = endpoint.OnConnected(connectionState.Connection);
                    }

                    var longPolling = new LongPolling(connectionState.Connection);

                    // Register this transport for disconnect
                    RegisterDisconnect(context, longPolling);

                    // Start the transport
                    await longPolling.ProcessRequest(context);

                    _manager.MarkConnectionInactive(connectionState.Connection.ConnectionId);
                }
            }
        }

        private static void RegisterDisconnect(HttpContext context, IHttpTransport transport)
        {
            context.RequestAborted.Register(state => ((IHttpTransport)state).CloseAsync(), transport);
        }

        private Task ProcessGetId(HttpContext context)
        {
            // Reserve an id for this connection
            var state = _manager.ReserveConnection();

            // Get the bytes for the connection id
            var connectionIdBuffer = Encoding.UTF8.GetBytes(state.Connection.ConnectionId);

            // Write it out to the response with the right content length
            context.Response.ContentLength = connectionIdBuffer.Length;
            return context.Response.Body.WriteAsync(connectionIdBuffer, 0, connectionIdBuffer.Length);
        }

        private Task ProcessSend(HttpContext context)
        {
            var connectionId = context.Request.Query["id"];
            if (StringValues.IsNullOrEmpty(connectionId))
            {
                throw new InvalidOperationException("Missing connection id");
            }

            ConnectionState state;
            if (_manager.TryGetConnection(connectionId, out state))
            {
                // If we received an HTTP POST for the connection id and it's not an HttpChannel then fail.
                // You can't write to a TCP channel directly from here.
                var httpChannel = state.Connection.Channel as HttpChannel;

                if (httpChannel == null)
                {
                    throw new InvalidOperationException("No channel");
                }

                return context.Request.Body.CopyToAsync(httpChannel.Input);
            }

            return Task.CompletedTask;
        }

        private ConnectionState GetOrCreateConnection(HttpContext context)
        {
            var connectionId = context.Request.Query["id"];
            ConnectionState connectionState;

            // There's no connection id so this is a branch new connection
            if (StringValues.IsNullOrEmpty(connectionId))
            {
                var channel = new HttpChannel(_channelFactory);
                connectionState = _manager.AddNewConnection(channel);
            }
            else
            {
                // REVIEW: Fail if not reserved? Reused an existing connection id?

                // There's a connection id
                if (!_manager.TryGetConnection(connectionId, out connectionState))
                {
                    throw new InvalidOperationException("Unknown connection id");
                }

                // Reserved connection, we need to provide a channel
                if (connectionState.Connection.Channel == null)
                {
                    connectionState.Connection.Channel = new HttpChannel(_channelFactory);
                    connectionState.LastSeen = DateTimeOffset.UtcNow;
                }
            }

            return connectionState;
        }
    }
}
