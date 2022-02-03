// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Features;

namespace Microsoft.AspNetCore.Http.Connections;

/// <summary>
/// Extension method to get the underlying <see cref="HttpContext"/> of the connection if there is one.
/// </summary>
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
    public static HttpContext? GetHttpContext(this ConnectionContext connection)
    {
        return connection.Features.Get<IHttpContextFeature>()?.HttpContext;
    }
}
