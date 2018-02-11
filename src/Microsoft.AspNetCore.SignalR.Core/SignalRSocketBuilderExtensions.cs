// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Protocols;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR
{
    public static class SignalRSocketBuilderExtensions
    {
        public static IConnectionBuilder UseHub<THub>(this IConnectionBuilder socketBuilder) where THub : Hub
        {
            var endpoint = socketBuilder.ApplicationServices.GetRequiredService<HubEndPoint<THub>>();
            return socketBuilder.Run(connection => endpoint.OnConnectedAsync(connection));
        }
    }
}
