// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Information regarding the TCP/IP connection carrying the request.
/// </summary>
public interface IHttpConnectionFeature
{
    /// <summary>
    /// Gets or sets the unique identifier for the connection the request was received on. This is primarily for diagnostic purposes.
    /// </summary>
    string ConnectionId { get; set; }

    /// <summary>
    /// Gets or sets the IPAddress of the client making the request. Note this may be for a proxy rather than the end user.
    /// </summary>
    IPAddress? RemoteIpAddress { get; set; }

    /// <summary>
    /// Gets or sets the local IPAddress on which the request was received.
    /// </summary>
    IPAddress? LocalIpAddress { get; set; }

    /// <summary>
    /// Gets or sets the remote port of the client making the request.
    /// </summary>
    int RemotePort { get; set; }

    /// <summary>
    /// Gets or sets the local port on which the request was received.
    /// </summary>
    int LocalPort { get; set; }
}
