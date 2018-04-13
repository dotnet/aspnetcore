// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Internal;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Http.Connections
{
    public class ConnectionsRouteBuilder
    {
        private readonly HttpConnectionDispatcher _dispatcher;
        private readonly RouteBuilder _routes;

        public ConnectionsRouteBuilder(RouteBuilder routes, HttpConnectionDispatcher dispatcher)
        {
            _routes = routes;
            _dispatcher = dispatcher;
        }

        public void MapConnections(PathString path, Action<IConnectionBuilder> configure) =>
            MapConnections(path, new HttpConnectionDispatcherOptions(), configure);

        public void MapConnections(PathString path, HttpConnectionDispatcherOptions options, Action<IConnectionBuilder> configure)
        {
            var connectionBuilder = new ConnectionBuilder(_routes.ServiceProvider);
            configure(connectionBuilder);
            var socket = connectionBuilder.Build();
            _routes.MapRoute(path, c => _dispatcher.ExecuteAsync(c, options, socket));
            _routes.MapRoute(path + "/negotiate", c => _dispatcher.ExecuteNegotiateAsync(c, options));
        }

        public void MapConnectionHandler<TConnectionHandler>(PathString path) where TConnectionHandler : ConnectionHandler
        {
            MapConnectionHandler<TConnectionHandler>(path, configureOptions: null);
        }

        public void MapConnectionHandler<TConnectionHandler>(PathString path, Action<HttpConnectionDispatcherOptions> configureOptions) where TConnectionHandler : ConnectionHandler
        {
            var authorizeAttributes = typeof(TConnectionHandler).GetCustomAttributes<AuthorizeAttribute>(inherit: true);
            var options = new HttpConnectionDispatcherOptions();
            foreach (var attribute in authorizeAttributes)
            {
                options.AuthorizationData.Add(attribute);
            }
            configureOptions?.Invoke(options);

            MapConnections(path, options, builder =>
            {
                builder.UseConnectionHandler<TConnectionHandler>();
            });
        }
    }
}
