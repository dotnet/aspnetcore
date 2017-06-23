// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Sockets;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubRouteBuilder
    {
        private readonly SocketRouteBuilder _routes;

        public HubRouteBuilder(SocketRouteBuilder routes)
        {
            _routes = routes;
        }

        public void MapHub<THub>(string path) where THub : Hub<IClientProxy>
        {
            MapHub<THub>(path, socketOptions: null);
        }

        public void MapHub<THub>(string path, Action<HttpSocketOptions> socketOptions) where THub : Hub<IClientProxy>
        {
            // find auth attributes
            var authorizeAttributes = typeof(THub).GetCustomAttributes<AuthorizeAttribute>(inherit: true);
            var options = new HttpSocketOptions();
            foreach (var attribute in authorizeAttributes)
            {
                options.AuthorizationData.Add(attribute);
            }
            socketOptions?.Invoke(options);

            _routes.MapSocket(path, options, builder =>
            {
                builder.UseHub<THub>();
            });
        }
    }
}
