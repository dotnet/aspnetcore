// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Features;

namespace Microsoft.AspNetCore.Http.Connections
{
    public static class HttpConnectionContextExtensions
    {
        /// <summary>
        /// Gets the <see cref="HttpContext"/> associated with the connection, if there is one.
        /// </summary>
        /// <param name="connection">The <see cref="ConnectionContext"/> representing the connection.</param>
        /// <returns>The <see cref="HttpContext"/> associated with the connection, or <see langword="null"/> if the connection is not HTTP-based.</returns>
        /// <remarks>
        /// SignalR connections can run on top of HTTP transports like WebSockets or Long Polling, or other non-HTTP transports. As a result,
        /// this method can sometimes return <see langword="null"/> depending on the configuration of your application.
        /// </remarks>
        public static HttpContext GetHttpContext(this ConnectionContext connection)
        {
            return connection.Features.Get<IHttpContextFeature>()?.HttpContext;
        }
    }
}
