// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// Maps incoming requests to <see cref="Hub"/> types.
    /// <para>
    ///     This class is obsolete and will be removed in a future version.
    ///     The recommended alternative is to use MapHub&#60;THub&#62; inside Microsoft.AspNetCore.Builder.UseEndpoints(...).
    /// </para>
    /// </summary>
    [Obsolete("This class is obsolete and will be removed in a future version. The recommended alternative is to use MapHub<THub> inside Microsoft.AspNetCore.Builder.UseEndpoints(...).")]
    public class HubRouteBuilder
    {
        private readonly ConnectionsRouteBuilder _routes;
        private readonly IEndpointRouteBuilder _endpoints;

        /// <summary>
        /// Initializes a new instance of the <see cref="HubRouteBuilder"/> class.
        /// </summary>
        /// <param name="routes">The routes builder.</param>
        public HubRouteBuilder(ConnectionsRouteBuilder routes)
        {
            _routes = routes;
        }

        internal HubRouteBuilder(IEndpointRouteBuilder endpoints)
        {
            _endpoints = endpoints;
        }

        /// <summary>
        /// Maps incoming requests with the specified path to the specified <see cref="Hub"/> type.
        /// </summary>
        /// <typeparam name="THub">The <see cref="Hub"/> type to map requests to.</typeparam>
        /// <param name="path">The request path.</param>
        public void MapHub<THub>(PathString path) where THub : Hub
        {
            MapHub<THub>(path, configureOptions: null);
        }

        /// <summary>
        /// Maps incoming requests with the specified path to the specified <see cref="Hub"/> type.
        /// </summary>
        /// <typeparam name="THub">The <see cref="Hub"/> type to map requests to.</typeparam>
        /// <param name="path">The request path.</param>
        /// <param name="configureOptions">A callback to configure dispatcher options.</param>
        public void MapHub<THub>(PathString path, Action<HttpConnectionDispatcherOptions> configureOptions) where THub : Hub
        {
            // This will be null if someone is manually using the HubRouteBuilder(ConnectionsRouteBuilder routes) constructor
            // SignalR itself will only use the IEndpointRouteBuilder overload
            if (_endpoints != null)
            {
                _endpoints.MapHub<THub>(path, configureOptions);
                return;
            }

            // find auth attributes
            var authorizeAttributes = typeof(THub).GetCustomAttributes<AuthorizeAttribute>(inherit: true);
            var options = new HttpConnectionDispatcherOptions();
            foreach (var attribute in authorizeAttributes)
            {
                options.AuthorizationData.Add(attribute);
            }
            configureOptions?.Invoke(options);

            _routes.MapConnections(path, options, builder =>
            {
                builder.UseHub<THub>();
            });
        }
    }
}
