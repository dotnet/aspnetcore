using System;
using Channels;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class HttpDispatcherAppBuilderExtensions
    {
        public static IApplicationBuilder UseSockets(this IApplicationBuilder app, Action<EndPointBuilder> callback)
        {
            var manager = new ConnectionManager();
            var factory = new ChannelFactory();
            var dispatcher = new HttpConnectionDispatcher(manager, factory);
            var routes = new RouteBuilder(app);

            callback(new EndPointBuilder(routes, dispatcher, app.ApplicationServices));

            // TODO: Use new low allocating websocket API
            app.UseWebSockets();
            app.UseRouter(routes.Build());
            return app;
        }

        public class EndPointBuilder
        {
            private readonly HttpConnectionDispatcher _dispatcher;
            private readonly RouteBuilder _routes;
            private readonly IServiceProvider _serviceProvider;

            public EndPointBuilder(RouteBuilder routes, HttpConnectionDispatcher dispatcher, IServiceProvider serviceProvider)
            {
                _routes = routes;
                _dispatcher = dispatcher;
                _serviceProvider = serviceProvider;
            }

            public EndPointConfiguration<TEndPoint> Configure<TEndPoint>() where TEndPoint : EndPoint
            {
                var socketFormatters = _serviceProvider.GetRequiredService<SocketFormatters>();
                return new EndPointConfiguration<TEndPoint>(_routes, _dispatcher, socketFormatters.GetEndPointFormatters<TEndPoint>());
            }
        }

        public class EndPointConfiguration<TEndPoint> where TEndPoint : EndPoint
        {
            private readonly HttpConnectionDispatcher _dispatcher;
            private readonly RouteBuilder _routes;
            private readonly EndPointFormatters _formatters;

            public EndPointConfiguration(RouteBuilder routes, HttpConnectionDispatcher dispatcher, EndPointFormatters formatters)
            {
                _routes = routes;
                _dispatcher = dispatcher;
                _formatters = formatters;
            }

            public EndPointConfiguration<TEndPoint> MapRoute(string path)
            {
                _routes.AddPrefixRoute(path, new RouteHandler(c => _dispatcher.Execute<TEndPoint>(path, c)));
                return this;
            }

            public EndPointConfiguration<TEndPoint> MapFormatter<T, TFormatterType>(string format)
                where TFormatterType : IFormatter<T>
            {
                _formatters.RegisterFormatter<T, TFormatterType>(format);
                return this;
            }
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
