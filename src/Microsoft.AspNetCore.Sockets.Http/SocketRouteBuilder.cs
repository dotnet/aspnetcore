// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Sockets
{
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
            _routes.MapRoute(path + "/negotiate", c => _dispatcher.ExecuteNegotiateAsync(c, options));
        }

        public void MapEndPoint<TEndPoint>(string path) where TEndPoint : EndPoint
        {
            MapEndPoint<TEndPoint>(path, socketOptions: null);
        }

        public void MapEndPoint<TEndPoint>(string path, Action<HttpSocketOptions> socketOptions) where TEndPoint : EndPoint
        {
            var authorizeAttributes = typeof(TEndPoint).GetCustomAttributes<AuthorizeAttribute>(inherit: true);
            var options = new HttpSocketOptions();
            foreach (var attribute in authorizeAttributes)
            {
                options.AuthorizationData.Add(attribute);
            }
            socketOptions?.Invoke(options);

            MapSocket(path, options, builder =>
            {
                builder.UseEndPoint<TEndPoint>();
            });
        }
    }
}
