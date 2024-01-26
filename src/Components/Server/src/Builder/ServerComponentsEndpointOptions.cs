// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Connections;

namespace Microsoft.AspNetCore.Components.Server;

/// <summary>
/// Options to configure interactive Server components.
/// </summary>
public class ServerComponentsEndpointOptions
{
    /// <summary>
    /// Gets or sets a value that indicates whether compression is enabled for the WebSocket connections.
    /// </summary>
    public bool EnableWebSocketCompression { get; set; }

    /// <summary>
    /// Gets or sets a callback to configure the underlying <see cref="HttpConnectionDispatcherOptions"/>.
    /// If set, this callback takes precedence over <see cref="EnableWebSocketCompression"/>.
    /// </summary>
    public Action<HttpConnectionDispatcherOptions> ConnectionOptions { get; set; }
}
