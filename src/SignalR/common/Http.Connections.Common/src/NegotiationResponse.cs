// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Abstractions;

namespace Microsoft.AspNetCore.Http.Connections;

/// <summary>
/// A response to a '/negotiate' request.
/// </summary>
public class NegotiationResponse
{
    /// <summary>
    /// An optional Url to redirect the client to another endpoint.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// An optional access token to go along with the Url.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// The public ID for the connection.
    /// </summary>
    public string? ConnectionId { get; set; }

    /// <summary>
    /// The private ID for the connection.
    /// </summary>
    public string? ConnectionToken { get; set; }

    /// <summary>
    /// The minimum value between the version the client sends and the maximum version the server supports.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// A list of transports the server supports.
    /// </summary>
    public IList<AvailableTransport>? AvailableTransports { get; set; }

    /// <summary>
    /// An optional error during the negotiate. If this is not null the other properties on the response can be ignored.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// If set, the connection should attempt to reconnect with the same <see cref="BaseConnectionContext.ConnectionId"/> if it disconnects.
    /// It should also set <see cref="IStatefulReconnectFeature"/> on the <see cref="BaseConnectionContext.Features"/> collection so other layers of the
    /// application (like SignalR) can react.
    /// </summary>
    public bool UseStatefulReconnect { get; set; }
}
