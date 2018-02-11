// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Protocols;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Sockets
{
    public static class ConnectionBuilderExtensions
    {
        public static IConnectionBuilder UseEndPoint<TEndPoint>(this IConnectionBuilder connectionBuilder) where TEndPoint : EndPoint
        {
            var endpoint = connectionBuilder.ApplicationServices.GetRequiredService<TEndPoint>();
            // This is a terminal middleware, so there's no need to use the 'next' parameter
            return connectionBuilder.Run(connection => endpoint.OnConnectedAsync(connection));
        }
    }
}
