// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Protocols;

namespace Microsoft.AspNetCore.Sockets
{
    public static class ConnectionBuilderExtensions
    {
        public static IConnectionBuilder UseEndPoint<TEndPoint>(this IConnectionBuilder connectionBuilder) where TEndPoint : EndPoint
        {
            var endpoint = (TEndPoint)connectionBuilder.ApplicationServices.GetService(typeof(TEndPoint));

            if (endpoint == null)
            {
                throw new InvalidOperationException($"{nameof(EndPoint)} type {typeof(TEndPoint)} is not registered.");
            }
            // This is a terminal middleware, so there's no need to use the 'next' parameter
            return connectionBuilder.Run(connection => endpoint.OnConnectedAsync(connection));
        }
    }
}
