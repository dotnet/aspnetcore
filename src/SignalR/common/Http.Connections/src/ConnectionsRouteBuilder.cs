// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Http.Connections
{
    /// <summary>
    /// Maps routes to ASP.NET Core Connection Handlers.
    /// <para>
    /// This class is obsolete and will be removed in a future version.
    /// The recommended alternative is to use MapConnection and MapConnectionHandler&#60;TConnectionHandler&#62; inside Microsoft.AspNetCore.Builder.UseEndpoints(...).
    /// </para>
    /// </summary>
    [Obsolete("This class is obsolete and will be removed in a future version. The recommended alternative is to use MapConnection and MapConnectionHandler<TConnectionHandler> inside Microsoft.AspNetCore.Builder.UseEndpoints(...).")]
    public class ConnectionsRouteBuilder
    {
        private readonly IEndpointRouteBuilder _endpoints;

        internal ConnectionsRouteBuilder(IEndpointRouteBuilder endpoints)
        {
            _endpoints = endpoints;
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
        public void MapConnections(PathString path, HttpConnectionDispatcherOptions options, Action<IConnectionBuilder> configure) =>
            _endpoints.MapConnections(path, options, configure);

        /// <summary>
        /// Maps incoming requests with the specified path to the provided connection pipeline.
        /// </summary>
        /// <typeparam name="TConnectionHandler">The <see cref="ConnectionHandler"/> type.</typeparam>
        /// <param name="path">The request path.</param>
        public void MapConnectionHandler<TConnectionHandler>(PathString path) where TConnectionHandler : ConnectionHandler =>
            MapConnectionHandler<TConnectionHandler>(path, configureOptions: null);

        /// <summary>
        /// Maps incoming requests with the specified path to the provided connection pipeline.
        /// </summary>
        /// <typeparam name="TConnectionHandler">The <see cref="ConnectionHandler"/> type.</typeparam>
        /// <param name="path">The request path.</param>
        /// <param name="configureOptions">A callback to configure dispatcher options.</param>
        public void MapConnectionHandler<TConnectionHandler>(PathString path, Action<HttpConnectionDispatcherOptions> configureOptions) where TConnectionHandler : ConnectionHandler =>
            _endpoints.MapConnectionHandler<TConnectionHandler>(path, configureOptions);
    }
}
