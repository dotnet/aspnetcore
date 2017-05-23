// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class HttpDispatcherAppBuilderExtensions
    {
        public static IApplicationBuilder UseSockets(this IApplicationBuilder app, Action<SocketRouteBuilder> callback)
        {
            var dispatcher = app.ApplicationServices.GetRequiredService<HttpConnectionDispatcher>();

            var routes = new RouteBuilder(app);

            callback(new SocketRouteBuilder(routes, dispatcher));

            app.UseWebSocketConnections();
            app.UseRouter(routes.Build());
            return app;
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

        public void MapSocket(string path, Action<ISocketBuilder> socketConfig) =>
            MapSocket(path, new HttpSocketOptions(), socketConfig);

        public void MapSocket(string path, HttpSocketOptions options, Action<ISocketBuilder> socketConfig)
        {
            var socketBuilder = new SocketBuilder(_routes.ServiceProvider);
            socketConfig(socketBuilder);
            var socket = socketBuilder.Build();
            _routes.MapRoute(path, c => _dispatcher.ExecuteAsync(c, options, socket));
        }

        public void MapEndPoint<TEndPoint>(string path) where TEndPoint : EndPoint
        {
            MapSocket(path, builder =>
            {
                builder.UseEndPoint<TEndPoint>();
            });
        }
    }
}
