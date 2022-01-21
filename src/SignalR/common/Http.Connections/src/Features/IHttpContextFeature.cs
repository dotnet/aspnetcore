// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Http.Connections.Features;

/// <summary>
/// Feature set on the <see cref="ConnectionContext"/> that provides access to the underlying <see cref="Http.HttpContext"/>
/// associated with the connection if there is one.
/// </summary>
public interface IHttpContextFeature
{
    /// <summary>
    /// The <see cref="Http.HttpContext"/> associated with the connection if available.
    /// </summary>
    /// <remarks>
    /// Connections can run on top of HTTP transports like WebSockets or Long Polling, or other non-HTTP transports. As a result,
    /// this API can sometimes return <see langword="null"/> depending on the configuration of your application.
    /// </remarks>
    HttpContext? HttpContext { get; set; }
}
