// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
            _routes.MapSocket(path, builder =>
            {
                builder.UseHub<THub>();
            });
        }
    }
}
