using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using WebApplication95.Routing;

namespace WebApplication95
{
    public static class DispatcherExtensions
    {
        public static IApplicationBuilder UseRealTimeConnections(this IApplicationBuilder app, Action<Dispatcher> callback)
        {
            var dispatcher = new Dispatcher(app);
            callback(dispatcher);
            app.UseRouter(dispatcher.GetRouter());
            return app;
        }
    }

    public class Dispatcher
    {
        private readonly ConnectionManager _manager = new ConnectionManager();
        private readonly RouteBuilder _routes;

        public Dispatcher(IApplicationBuilder app)
        {
            _routes = new RouteBuilder(app);
        }

        public void MapEndPoint<TEndPoint>(string path) where TEndPoint : EndPoint
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

                var endpoint = (EndPoint)context.RequestServices.GetRequiredService<TEndPoint>();

                // Outgoing channels
                if (context.Request.Path.StartsWithSegments(path + "/sse"))
                {
                    var connectionState = GetOrCreateConnection(context);
                    var sse = new ServerSentEvents(connectionState);

                    var ignore = endpoint.OnConnected(connectionState.Connection);

                    connectionState.Connection.TransportType = TransportType.ServerSentEvents;

                    await sse.ProcessRequest(context);

                    connectionState.Connection.Complete();

                    _manager.RemoveConnection(connectionState.Connection.ConnectionId);
                }
                else if (context.Request.Path.StartsWithSegments(path + "/ws"))
                {
                    var connectionState = GetOrCreateConnection(context);
                    var ws = new WebSockets(connectionState);

                    var ignore = endpoint.OnConnected(connectionState.Connection);

                    connectionState.Connection.TransportType = TransportType.WebSockets;

                    await ws.ProcessRequest(context);

                    connectionState.Connection.Complete();

                    _manager.RemoveConnection(connectionState.Connection.ConnectionId);
                }
                else if (context.Request.Path.StartsWithSegments(path + "/poll"))
                {
                    var connectionId = context.Request.Query["id"];
                    ConnectionState connectionState;
                    bool newConnection = false;
                    if (_manager.AddConnection(connectionId, out connectionState))
                    {
                        newConnection = true;
                        var ignore = endpoint.OnConnected(connectionState.Connection);

                        connectionState.Connection.TransportType = TransportType.LongPolling;
                    }

                    var longPolling = new LongPolling(connectionState);

                    await longPolling.ProcessRequest(newConnection, context);

                    _manager.MarkConnectionDead(connectionState.Connection.ConnectionId);
                }
            }
        }

        private async Task ProcessGetId(HttpContext context)
        {
            var connectionId = _manager.GetConnectionId(context);
            ConnectionState state;
            _manager.AddConnection(connectionId, out state);
            context.Response.Headers["X-SignalR-ConnectionId"] = connectionId;
            await context.Response.WriteAsync($"{{ \"connectionId\": \"{connectionId}\" }}");
            return;
        }

        private async Task ProcessSend(HttpContext context)
        {
            var connectionId = context.Request.Query["id"];
            if (StringValues.IsNullOrEmpty(connectionId))
            {
                throw new InvalidOperationException("Missing connection id");
            }

            ConnectionState state;
            if (_manager.TryGetConnection(connectionId, out state))
            {
                // Write the message length
                await context.Request.Body.CopyToAsync(state.Connection.Input);
            }
        }

        private ConnectionState GetOrCreateConnection(HttpContext context)
        {
            var connectionId = context.Request.Query["id"];
            ConnectionState connectionState;

            if (StringValues.IsNullOrEmpty(connectionId))
            {
                connectionId = _manager.GetConnectionId(context);
                _manager.AddConnection(connectionId, out connectionState);
            }
            else
            {
                if (!_manager.TryGetConnection(connectionId, out connectionState))
                {
                    throw new InvalidOperationException("Unknown connection id");
                }
            }

            return connectionState;
        }
    }
}
