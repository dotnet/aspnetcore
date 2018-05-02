// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Internal;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Http.Connections
{
    /// <summary>
    /// Maps routes to ASP.NET Core Connection Handlers.
    /// </summary>
    public class ConnectionsRouteBuilder
    {
        private readonly HttpConnectionDispatcher _dispatcher;
        private readonly RouteBuilder _routes;

        internal ConnectionsRouteBuilder(RouteBuilder routes, HttpConnectionDispatcher dispatcher)
        {
            _routes = routes;
            _dispatcher = dispatcher;
        }

        /// <summary>
        /// Maps incoming requests with the specified path to the provided connection pipeline.
        /// </summary>
        /// <param name="path">The request path.</param>
        /// <param name="configure">A callback to configure the connection.</param>
        public void MapConnections(PathString path, Action<IConnectionBuilder> configure) =>
            MapConnections(path, new HttpConnectionDispatcherOptions(), configure);

        /// <summary>
        /// Maps incoming requests with the specified path to the provided connection pipeline.
        /// </summary>
        /// <param name="path">The request path.</param>
        /// <param name="options">Options used to configure the connection.</param>
        /// <param name="configure">A callback to configure the connection.</param>
        public void MapConnections(PathString path, HttpConnectionDispatcherOptions options, Action<IConnectionBuilder> configure)
        {
            var connectionBuilder = new ConnectionBuilder(_routes.ServiceProvider);
            configure(connectionBuilder);
            var socket = connectionBuilder.Build();
            _routes.MapRoute(path, c => _dispatcher.ExecuteAsync(c, options, socket));
            _routes.MapRoute(path + "/negotiate", c => _dispatcher.ExecuteNegotiateAsync(c, options));
        }

        /// <summary>
        /// Maps incoming requests with the specified path to the provided connection pipeline.
        /// </summary>
        /// <typeparam name="TConnectionHandler">The <see cref="ConnectionHandler"/> type.</typeparam>
        /// <param name="path">The request path.</param>
        public void MapConnectionHandler<TConnectionHandler>(PathString path) where TConnectionHandler : ConnectionHandler
        {
            MapConnectionHandler<TConnectionHandler>(path, configureOptions: null);
        }

        /// <summary>
        /// Maps incoming requests with the specified path to the provided connection pipeline.
        /// </summary>
        /// <typeparam name="TConnectionHandler">The <see cref="ConnectionHandler"/> type.</typeparam>
        /// <param name="path">The request path.</param>
        /// <param name="configureOptions">A callback to configure dispatcher options.</param>
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
