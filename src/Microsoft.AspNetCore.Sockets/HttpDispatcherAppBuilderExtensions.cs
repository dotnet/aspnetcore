// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Routing;
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

        public void MapEndpoint<TEndPoint>(string path) where TEndPoint : EndPoint
        {
            _routes.AddPrefixRoute(path, new RouteHandler(c => _dispatcher.ExecuteAsync<TEndPoint>(path, c)));
        }
    }
}
