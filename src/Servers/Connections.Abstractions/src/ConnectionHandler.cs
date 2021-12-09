// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Connections;

/// <summary>
/// Represents an endpoint that multiple connections connect to. For HTTP, endpoints are URLs, for non-HTTP it can be a TCP listener (or similar).
/// </summary>
public abstract class ConnectionHandler
{
    /// <summary>
    /// Called when a new connection is accepted to the endpoint.
    /// </summary>
    /// <param name="connection">The new <see cref="ConnectionContext"/></param>
    /// <returns>A <see cref="Task"/> that represents the connection lifetime. When the task completes, the connection is complete.</returns>
    public abstract Task OnConnectedAsync(ConnectionContext connection);
}
