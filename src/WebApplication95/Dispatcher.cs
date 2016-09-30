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
            if (context.Request.Path.StartsWithSegments(path + "/send"))
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
            else
            {
                var endpoint = (EndPoint)context.RequestServices.GetRequiredService<TEndPoint>();

                var connectionId = _manager.GetConnectionId(context);

                // Outgoing channels
                if (context.Request.Path.StartsWithSegments(path + "/sse"))
                {
                    ConnectionState state;
                    _manager.AddConnection(connectionId, out state);

                    var sse = new ServerSentEvents(state);

                    var ignore = endpoint.OnConnected(state.Connection);

                    state.Connection.TransportType = TransportType.ServerSentEvents;

                    await sse.ProcessRequest(context);

                    state.Connection.Complete();

                    _manager.RemoveConnection(connectionId);
                }
                else if (context.Request.Path.StartsWithSegments(path + "/ws"))
                {
                    ConnectionState state;
                    _manager.AddConnection(connectionId, out state);

                    var ws = new WebSockets(state);

                    var ignore = endpoint.OnConnected(state.Connection);

                    state.Connection.TransportType = TransportType.WebSockets;

                    await ws.ProcessRequest(context);

                    state.Connection.Complete();

                    _manager.RemoveConnection(connectionId);
                }
                else if (context.Request.Path.StartsWithSegments(path + "/poll"))
                {
                    ConnectionState state;
                    bool newConnection = false;
                    if (_manager.AddConnection(connectionId, out state))
                    {
                        newConnection = true;
                        var ignore = endpoint.OnConnected(state.Connection);

                        state.Connection.TransportType = TransportType.LongPolling;
                    }

                    var longPolling = new LongPolling(state);

                    await longPolling.ProcessRequest(newConnection, context);

                    _manager.MarkConnectionDead(connectionId);
                }

            }
        }
    }
}
