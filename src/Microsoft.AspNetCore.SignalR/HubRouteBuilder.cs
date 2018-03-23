// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Sockets;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubRouteBuilder
    {
        private readonly ConnectionsRouteBuilder _routes;

        public HubRouteBuilder(ConnectionsRouteBuilder routes)
        {
            _routes = routes;
        }

        public void MapHub<THub>(string path) where THub : Hub
        {
            MapHub<THub>(new PathString(path), configureOptions: null);
        }

        public void MapHub<THub>(PathString path) where THub : Hub
        {
            MapHub<THub>(path, configureOptions: null);
        }

        public void MapHub<THub>(PathString path, Action<HttpConnectionOptions> configureOptions) where THub : Hub
        {
            // find auth attributes
            var authorizeAttributes = typeof(THub).GetCustomAttributes<AuthorizeAttribute>(inherit: true);
            var options = new HttpConnectionOptions();
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
