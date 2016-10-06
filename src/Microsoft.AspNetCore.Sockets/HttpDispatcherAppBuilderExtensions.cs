using System;
using Channels;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Routing;

namespace Microsoft.AspNetCore.Builder
{
    public static class HttpDispatcherAppBuilderExtensions
    {
        public static IApplicationBuilder UseSockets(this IApplicationBuilder app, Action<SocketRouteBuilder> callback)
        {
            var manager = new ConnectionManager();
            var factory = new ChannelFactory();
            var dispatcher = new HttpConnectionDispatcher(manager, factory);
            var routes = new RouteBuilder(app);

            callback(new SocketRouteBuilder(routes, dispatcher));

            // TODO: Use new low allocating websocket API
            app.UseWebSockets();
            app.UseRouter(routes.Build());
            return app;
        }

        public class SocketRouteBuilder
        {
            private readonly HttpConnectionDispatcher _dispatcher;
            private readonly RouteBuilder _routes;

            public SocketRouteBuilder(RouteBuilder routes, HttpConnectionDispatcher dispatcher)
            {
                _routes = routes;
                _dispatcher = dispatcher;
            }

            public void MapSocketEndpoint<TEndPoint>(string path) where TEndPoint : EndPoint
            {
                _routes.AddPrefixRoute(path, new RouteHandler(c => _dispatcher.Execute<TEndPoint>(path, c)));

            }
        }
    }
}
