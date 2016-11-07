// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Channels;
using Microsoft.AspNetCore.Hosting;
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
            var manager = new ConnectionManager();
            var factory = new ChannelFactory();

            var dispatcher = new HttpConnectionDispatcher(manager, factory);

            // Dispose the connection manager when application shutdown is triggered
            var lifetime = app.ApplicationServices.GetRequiredService<IApplicationLifetime>();
            lifetime.ApplicationStopping.Register(state => ((IDisposable)state).Dispose(), manager);

            var routes = new RouteBuilder(app);

            callback(new SocketRouteBuilder(routes, dispatcher));

            // TODO: Use new low allocating websocket API
            app.UseWebSockets();
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
